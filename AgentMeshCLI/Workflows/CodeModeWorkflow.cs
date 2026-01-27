using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;

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

            _logger.LogInformation("Engaging Context Manager Agent...");

            var contextManagerOutput = await _contextManagerAgent.ExecuteAsync(new ContextManagerAgentInput
            {
                UserSentenceText = state.UserQuestion
            });
            state.ContextManagerResponse = contextManagerOutput.ContextEnrichedUserSentenceText;
            state.AddTokenUsage(ContextManagerAgentConfiguration.AgentName, contextManagerOutput.TokenCount, contextManagerOutput.InputTokenCount, contextManagerOutput.OutputTokenCount);

            _logger.LogInformation("Engaging Translator Agent...");

            var translatorOutput = await _translatorAgent.ExecuteAsync(new TranslatorAgentInput
            {
                Sentence = state.ContextManagerResponse,
                TargetLanguage = INTERNAL_AGENTIC_LANGUAGE
            });
            state.TranslatorResponse = translatorOutput.TranslatedSentence;
            state.DetectedOriginalLanguage = translatorOutput.DetectedOriginalLanguage;
            state.AddTokenUsage(TranslatorAgentConfiguration.AgentName, translatorOutput.TokenCount, translatorOutput.InputTokenCount, translatorOutput.OutputTokenCount);

            _logger.LogInformation("Engaging Router Agent...");

            var routerOutput = await _routerAgent.ExecuteAsync(new RouterAgentInput
            {
                Message = state.TranslatorResponse
            });
            state.RouterRecipient = routerOutput.Recipient;
            state.AddTokenUsage(RouterAgentConfiguration.AgentName, routerOutput.TokenCount, routerOutput.InputTokenCount, routerOutput.OutputTokenCount);

            if (routerOutput.Recipient?.Equals("Personal Assistant", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Engaging Personal Assistant Agent...");

                var dataForPersonalAssistant = state.OutputForUserFromBusinessAnalyst ?? string.Empty;
                var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
                {
                    Sentence = state.TranslatorResponse ?? string.Empty,
                    Data = dataForPersonalAssistant,
                    TargetLanguage = state.DetectedOriginalLanguage ?? INTERNAL_AGENTIC_LANGUAGE
                });
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
                _logger.LogInformation("Engaging Business Requirements Creator Agent...");
                var brcOutput = await _businessRequirementsCreatorAgent.ExecuteAsync(new BusinessRequirementsCreatorAgentInput
                {
                    UserQuestionText = state.TranslatorResponse
                });
                state.ShouldEngageCoder = brcOutput.EngageCoderAgent;
                state.AddTokenUsage(BusinessRequirementsCreatorAgentConfiguration.AgentName, brcOutput.TokenCount, brcOutput.InputTokenCount, brcOutput.OutputTokenCount);

                if (brcOutput.EngageCoderAgent)
                {
                    _logger.LogInformation("Engaging Coder Agent...");
                    state.BusinessRequirements = brcOutput.BusinessRequirements;

                    var coderAgentOutput = await _coderAgent.ExecuteAsync(new CoderAgentInput
                    {
                        BusinessRequirements = state.BusinessRequirements ?? string.Empty
                    });
                    state.GeneratedCode = coderAgentOutput.CodeToRun;
                    state.AddTokenUsage(CoderAgentConfiguration.AgentName, coderAgentOutput.TokenCount, coderAgentOutput.InputTokenCount, coderAgentOutput.OutputTokenCount);

                    var codeWithLineNumbers = GetSourceCodeWithLineNumbers(coderAgentOutput.CodeToRun);
                    state.LastCodeWithLineNumbers = codeWithLineNumbers;
                    
                    _logger.LogInformation("Engaging Code Static Analyzer Agent...");
                    var staticAnalyzerOutput = await _codeStaticAnalyzer.ExecuteAsync(new CodeStaticAnalyzerInput
                    {
                        CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty
                    });
                    state.IsCodeValid = !staticAnalyzerOutput.Violations.Any();
                    if (!state.IsCodeValid)
                    {
                        state.CodeIssues = staticAnalyzerOutput.Violations.ToList();
                    }
                    state.AddTokenUsage(CodeStaticAnalyzerConfiguration.AgentName, staticAnalyzerOutput.TokenCount, staticAnalyzerOutput.InputTokenCount, staticAnalyzerOutput.OutputTokenCount);

                    for (int i = 0; i < 2 && !state.IsCodeValid && state.CodeIssues.Any(); i++)
                    {
                        _logger.LogInformation("Engaging Code Fixer Agent... Iteration {Iteration}", i + 1);
                        var codeFixerOutput = await _codeFixerAgent.ExecuteAsync(new CodeFixerAgentInput
                        {
                            CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty,
                            Issues = state.CodeIssues
                        });
                        state.GeneratedCode = codeFixerOutput.FixedCode;
                        state.CodeFixerIterationCount++;
                        state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.TokenCount, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount);

                        var fixedCodeWithLineNumbers = GetSourceCodeWithLineNumbers(codeFixerOutput.FixedCode);
                        state.LastCodeWithLineNumbers = fixedCodeWithLineNumbers;

                        var reAnalyzerOutput = await _codeStaticAnalyzer.ExecuteAsync(new CodeStaticAnalyzerInput
                        {
                            CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty
                        });
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
                        _logger.LogInformation("Engaging JS Sandbox Executor...");
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
                    _logger.LogInformation("Engaging Results Presenter Agent...");
                    var resultsPresenterOutput = await _resultsPresenterAgent.ExecuteAsync(new ResultsPresenterAgentInput
                    {
                        Content = executionResult
                    });
                    state.PresenterOutput = resultsPresenterOutput.Content;
                    state.AddTokenUsage(ResultsPresenterAgentConfiguration.AgentName, resultsPresenterOutput.TokenCount, resultsPresenterOutput.InputTokenCount, resultsPresenterOutput.OutputTokenCount);

                    var dataForPersonalAssistant = state.PresenterOutput ?? executionResult;
                    _logger.LogInformation("Engaging Personal Assistant Agent...");
                    var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
                    {
                        Sentence = state.TranslatorResponse ?? string.Empty,
                        Data = dataForPersonalAssistant,
                        TargetLanguage = state.DetectedOriginalLanguage ?? INTERNAL_AGENTIC_LANGUAGE
                    });
                    state.FinalAnswer = personalAssistantOutput.Response;
                    state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount);
                }
                else
               {
                    state.OutputForUserFromBusinessAnalyst = brcOutput.AnswerToUserText ?? string.Empty;

                    var dataForPersonalAssistant = state.OutputForUserFromBusinessAnalyst;
                    _logger.LogInformation("Engaging Personal Assistant Agent...");
                    var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
                    {
                        Sentence = state.TranslatorResponse ?? string.Empty,
                        Data = dataForPersonalAssistant,
                        TargetLanguage = state.DetectedOriginalLanguage ?? INTERNAL_AGENTIC_LANGUAGE
                    });
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
                _logger.LogInformation("Engaging Personal Assistant Agent...");
                var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
                {
                    Sentence = state.TranslatorResponse ?? string.Empty,
                    Data = dataForPersonalAssistant,
                    TargetLanguage = state.DetectedOriginalLanguage ?? INTERNAL_AGENTIC_LANGUAGE
                });
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
