using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using Polly;
using System.ClientModel;

namespace AgentMesh.Workflows
{
    public class CodeModeWorkflow : IWorkflow
    {
        private const string INTERNAL_AGENTIC_LANGUAGE = "English";

        private readonly ILogger<CodeModeWorkflow> _logger;

        private readonly IBusinessRequirementsCreatorAgent _businessRequirementsCreatorAgent;
        private readonly ICoderAgent _coderAgent;
        private readonly ICodeStaticAnalyzer _codeStaticAnalyzer;
        private readonly ICodeFixerAgent _codeFixerAgent;
        private readonly IResultsPresenterAgent _resultsPresenterAgent;
        private readonly IJSSandboxExecutor _jsSandboxExecutor;
        private readonly IContextManagerAgent _contextManagerAgent;
        private readonly ITranslatorAgent _translatorAgent;
        private readonly IRouterAgent _routerAgent;
        private readonly IPersonalAssistantAgent _personalAssistantAgent;

        public CodeModeWorkflow(ILogger<CodeModeWorkflow> logger,
            IBusinessRequirementsCreatorAgent businessRequirementsCreatorAgent,
            ICoderAgent coderAgent,
            ICodeStaticAnalyzer codeStaticAnalyzer,
            ICodeFixerAgent codeFixerAgent,
            IResultsPresenterAgent resultsPresenterAgent,
            IJSSandboxExecutor jsSandboxExecutor,
            IContextManagerAgent contextManagerAgent,
            ITranslatorAgent translatorAgent,
            IRouterAgent routerAgent,
            IPersonalAssistantAgent personalAssistantAgent)
        {
            _logger = logger;
            _businessRequirementsCreatorAgent = businessRequirementsCreatorAgent;
            _coderAgent = coderAgent;
            _codeStaticAnalyzer = codeStaticAnalyzer;
            _codeFixerAgent = codeFixerAgent;
            _resultsPresenterAgent = resultsPresenterAgent;
            _jsSandboxExecutor = jsSandboxExecutor;
            _contextManagerAgent = contextManagerAgent;
            _translatorAgent = translatorAgent;
            _routerAgent = routerAgent;
            _personalAssistantAgent = personalAssistantAgent;
        }

        public async Task<WorkflowResult> ExecuteAsync(string userInput)
        {
            var state = new CodeModeWorkflowState(userInput);

            var contextManagerOutput = await ExecuteWithRetryAsync(
                () => _contextManagerAgent.ExecuteAsync(new ContextManagerAgentInput
                {
                    UserSentenceText = state.UserQuestion
                }),
                ContextManagerAgentConfiguration.AgentName);
            state.ContextManagerResponse = contextManagerOutput.ContextEnrichedUserSentenceText;
            state.AddTokenUsage(ContextManagerAgentConfiguration.AgentName, contextManagerOutput.TokenCount, contextManagerOutput.InputTokenCount, contextManagerOutput.OutputTokenCount);

            var translatorOutput = await ExecuteWithRetryAsync(
                () => _translatorAgent.ExecuteAsync(new TranslatorAgentInput
                {
                    Sentence = state.ContextManagerResponse,
                    TargetLanguage = INTERNAL_AGENTIC_LANGUAGE
                }),
                TranslatorAgentConfiguration.AgentName);
            state.TranslatorResponse = translatorOutput.TranslatedSentence;
            state.DetectedOriginalLanguage = translatorOutput.DetectedOriginalLanguage;
            state.AddTokenUsage(TranslatorAgentConfiguration.AgentName, translatorOutput.TokenCount, translatorOutput.InputTokenCount, translatorOutput.OutputTokenCount);

            var routerOutput = await ExecuteWithRetryAsync(
                () => _routerAgent.ExecuteAsync(new RouterAgentInput
                {
                    Message = state.TranslatorResponse
                }),
                RouterAgentConfiguration.AgentName);
            state.RouterRecipient = routerOutput.Recipient;
            state.AddTokenUsage(RouterAgentConfiguration.AgentName, routerOutput.TokenCount, routerOutput.InputTokenCount, routerOutput.OutputTokenCount);

            if (routerOutput.Recipient?.Equals("Personal Assistant", StringComparison.OrdinalIgnoreCase) == true)
            {
                var dataForPersonalAssistant = state.OutputForUserFromBusinessAnalyst ?? string.Empty;
                var personalAssistantOutput = await ExecuteWithRetryAsync(
                    () => _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
                    {
                        Sentence = state.TranslatorResponse ?? string.Empty,
                        Data = dataForPersonalAssistant,
                        TargetLanguage = state.DetectedOriginalLanguage ?? INTERNAL_AGENTIC_LANGUAGE
                    }),
                    PersonalAssistantAgentConfiguration.AgentName);
                state.FinalAnswer = personalAssistantOutput.Response;
                state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount);

                var contextManagerState = await _contextManagerAgent.GetState();
                contextManagerState.ChatHistory.Add(new AgentMessage
                {
                    Role = AgentMessageRole.Assistant,
                    Content = state.FinalAnswer
                });
                await _contextManagerAgent.SetState(contextManagerState);
            }
            else if (routerOutput.Recipient?.Equals("Business Analyst", StringComparison.OrdinalIgnoreCase) == true)
            {
                var brcOutput = await ExecuteWithRetryAsync(
                    () => _businessRequirementsCreatorAgent.ExecuteAsync(new BusinessRequirementsCreatorAgentInput
                    {
                        UserQuestionText = state.TranslatorResponse
                    }),
                    BusinessRequirementsCreatorAgentConfiguration.AgentName);
                state.ShouldEngageCoder = brcOutput.EngageCoderAgent;
                state.AddTokenUsage(BusinessRequirementsCreatorAgentConfiguration.AgentName, brcOutput.TokenCount, brcOutput.InputTokenCount, brcOutput.OutputTokenCount);

                if (brcOutput.EngageCoderAgent)
                {
                    state.BusinessRequirements = brcOutput.BusinessRequirements;

                    var coderAgentOutput = await ExecuteWithRetryAsync(
                        () => _coderAgent.ExecuteAsync(new CoderAgentInput
                        {
                            BusinessRequirements = state.BusinessRequirements ?? string.Empty
                        }),
                        CoderAgentConfiguration.AgentName);
                    state.GeneratedCode = coderAgentOutput.CodeToRun;
                    state.AddTokenUsage(CoderAgentConfiguration.AgentName, coderAgentOutput.TokenCount, coderAgentOutput.InputTokenCount, coderAgentOutput.OutputTokenCount);

                    var codeWithLineNumbers = GetSourceCodeWithLineNumbers(coderAgentOutput.CodeToRun);
                    state.LastCodeWithLineNumbers = codeWithLineNumbers;

                    var staticAnalyzerOutput = await ExecuteWithRetryAsync(
                        () => _codeStaticAnalyzer.ExecuteAsync(new CodeStaticAnalyzerInput
                        {
                            CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty
                        }),
                        CodeStaticAnalyzerConfiguration.AgentName);
                    state.IsCodeValid = !staticAnalyzerOutput.Violations.Any();
                    if (!state.IsCodeValid)
                    {
                        state.CodeIssues = staticAnalyzerOutput.Violations.ToList();
                    }
                    state.AddTokenUsage(CodeStaticAnalyzerConfiguration.AgentName, staticAnalyzerOutput.TokenCount, staticAnalyzerOutput.InputTokenCount, staticAnalyzerOutput.OutputTokenCount);

                    for (int i = 0; i < 2 && !state.IsCodeValid && state.CodeIssues.Any(); i++)
                    {
                        var codeFixerOutput = await ExecuteWithRetryAsync(
                            () => _codeFixerAgent.ExecuteAsync(new CodeFixerAgentInput
                            {
                                CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty,
                                Issues = state.CodeIssues
                            }),
                            CodeFixerAgentConfiguration.AgentName);
                        state.GeneratedCode = codeFixerOutput.FixedCode;
                        state.CodeFixerIterationCount++;
                        state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.TokenCount, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount);

                        var fixedCodeWithLineNumbers = GetSourceCodeWithLineNumbers(codeFixerOutput.FixedCode);
                        state.LastCodeWithLineNumbers = fixedCodeWithLineNumbers;

                        var reAnalyzerOutput = await ExecuteWithRetryAsync(
                            () => _codeStaticAnalyzer.ExecuteAsync(new CodeStaticAnalyzerInput
                            {
                                CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty
                            }),
                            CodeStaticAnalyzerConfiguration.AgentName);
                        state.IsCodeValid = !reAnalyzerOutput.Violations.Any();
                        if (!state.IsCodeValid)
                        {
                            state.CodeIssues = reAnalyzerOutput.Violations.ToList();
                        }
                        else
                        {
                            state.CodeIssues.Clear();
                        }
                        state.AddTokenUsage(CodeStaticAnalyzerConfiguration.AgentName, reAnalyzerOutput.TokenCount, reAnalyzerOutput.InputTokenCount, reAnalyzerOutput.OutputTokenCount);
                    }

                    try
                    {
                        var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new JSSandboxInput
                        {
                            Code = state.GeneratedCode ?? string.Empty
                        });
                        state.SandboxResult = executionOutput.Result;
                        state.SandboxError = null;
                    }
                    catch (Exception ex)
                    {
                        state.SandboxError = ex.Message;
                        state.SandboxResult = null;
                    }

                    var executionResult = state.SandboxError ?? state.SandboxResult ?? string.Empty;
                    var resultsPresenterOutput = await ExecuteWithRetryAsync(
                        () => _resultsPresenterAgent.ExecuteAsync(new ResultsPresenterAgentInput
                        {
                            Content = executionResult
                        }),
                        ResultsPresenterAgentConfiguration.AgentName);
                    state.PresenterOutput = resultsPresenterOutput.Content;
                    state.AddTokenUsage(ResultsPresenterAgentConfiguration.AgentName, resultsPresenterOutput.TokenCount, resultsPresenterOutput.InputTokenCount, resultsPresenterOutput.OutputTokenCount);

                    var dataForPersonalAssistant = state.PresenterOutput ?? executionResult;
                    var personalAssistantOutput = await ExecuteWithRetryAsync(
                        () => _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
                        {
                            Sentence = state.TranslatorResponse ?? string.Empty,
                            Data = dataForPersonalAssistant,
                            TargetLanguage = state.DetectedOriginalLanguage ?? INTERNAL_AGENTIC_LANGUAGE
                        }),
                        PersonalAssistantAgentConfiguration.AgentName);
                    state.FinalAnswer = personalAssistantOutput.Response;
                    state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount);
                }
                else
                {
                    state.OutputForUserFromBusinessAnalyst = brcOutput.AnswerToUserText ?? string.Empty;

                    var dataForPersonalAssistant = state.OutputForUserFromBusinessAnalyst;
                    var personalAssistantOutput = await ExecuteWithRetryAsync(
                        () => _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
                        {
                            Sentence = state.TranslatorResponse ?? string.Empty,
                            Data = dataForPersonalAssistant,
                            TargetLanguage = state.DetectedOriginalLanguage ?? INTERNAL_AGENTIC_LANGUAGE
                        }),
                        PersonalAssistantAgentConfiguration.AgentName);
                    state.FinalAnswer = personalAssistantOutput.Response;
                    state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount);
                }

                var contextManagerState = await _contextManagerAgent.GetState();
                contextManagerState.ChatHistory.Add(new AgentMessage
                {
                    Role = AgentMessageRole.Assistant,
                    Content = state.FinalAnswer
                });
                await _contextManagerAgent.SetState(contextManagerState);
            }
            else
            {
                var dataForPersonalAssistant = string.Empty;
                var personalAssistantOutput = await ExecuteWithRetryAsync(
                    () => _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
                    {
                        Sentence = state.TranslatorResponse ?? string.Empty,
                        Data = dataForPersonalAssistant,
                        TargetLanguage = state.DetectedOriginalLanguage ?? INTERNAL_AGENTIC_LANGUAGE
                    }),
                    PersonalAssistantAgentConfiguration.AgentName);
                state.FinalAnswer = personalAssistantOutput.Response;
                state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount);

                var contextManagerState = await _contextManagerAgent.GetState();
                contextManagerState.ChatHistory.Add(new AgentMessage
                {
                    Role = AgentMessageRole.Assistant,
                    Content = state.FinalAnswer
                });
                await _contextManagerAgent.SetState(contextManagerState);
            }

            return new WorkflowResult
            {
                Response = state.FinalAnswer!,
                InputTokenUsage = state.InputTokenUsage,
                OutputTokenUsage = state.OutputTokenUsage
            };
        }

        private Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string agentName)
        {
            var policy = Policy
                .Handle<BadStructuredResponseException>()
                .Or<ClientResultException>(ex => ex.Message.Contains("Tool choice is none, but model called a tool"))
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: _ => TimeSpan.FromSeconds(5),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        // Silent retry for workflow execution
                        _logger.LogWarning(exception, "Retry {RetryCount} for agent {AgentName} due to error: {ErrorMessage}", retryCount, agentName, exception.Message);
                    });

            return policy.ExecuteAsync(action);
        }

        private static string GetSourceCodeWithLineNumbers(string sourceCode, bool useSpaceFiller = true)
        {
            var fillerChar = ' ';
            if (!useSpaceFiller)
            {
                fillerChar = '0';
            }
            var codeLines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var maxLineNumberWidth = codeLines.Length.ToString().Length;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < codeLines.Length; i++)
            {
                sb.AppendLine($"[{(i + 1).ToString().PadLeft(maxLineNumberWidth, fillerChar)}] {codeLines[i]}");
            }
            return sb.ToString();
        }
    }
}
