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
        private readonly IBusinessAdvisorAgent _businessAdvisorAgent;
        private readonly ICoderAgent _coderAgent;
        private readonly ICodeStaticAnalyzer _codeStaticAnalyzer;
        private readonly ICodeFixerAgent _codeFixerAgent;
        private readonly IResultsPresenterAgent _resultsPresenterAgent;
        private readonly IJSSandboxExecutor _jsSandboxExecutor;
        private readonly IContextManagerAgent _contextManagerAgent;
        private readonly ITranslatorAgent _translatorAgent;
        private readonly IContextAggregatorAgent _contextAggregatorAgent;
        private readonly IRouterAgent _routerAgent;
        private readonly IPersonalAssistantAgent _personalAssistantAgent;

        public CodeModeWorkflow(ILogger<CodeModeWorkflow> logger,
            IBusinessRequirementsCreatorAgent businessRequirementsCreatorAgent,
            IBusinessAdvisorAgent businessAdvisorAgent,
            ICoderAgent coderAgent,
            ICodeStaticAnalyzer codeStaticAnalyzer,
            ICodeFixerAgent codeFixerAgent,
            IResultsPresenterAgent resultsPresenterAgent,
            IJSSandboxExecutor jsSandboxExecutor,
            IContextManagerAgent contextManagerAgent,
            ITranslatorAgent translatorAgent,
            IContextAggregatorAgent contextAggregatorAgent,
            IRouterAgent routerAgent,
            IPersonalAssistantAgent personalAssistantAgent)
        {
            _logger = logger;
            _businessRequirementsCreatorAgent = businessRequirementsCreatorAgent;
            _businessAdvisorAgent = businessAdvisorAgent;
            _coderAgent = coderAgent;
            _codeStaticAnalyzer = codeStaticAnalyzer;
            _codeFixerAgent = codeFixerAgent;
            _resultsPresenterAgent = resultsPresenterAgent;
            _jsSandboxExecutor = jsSandboxExecutor;
            _contextManagerAgent = contextManagerAgent;
            _translatorAgent = translatorAgent;
            _contextAggregatorAgent = contextAggregatorAgent;
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
            if (contextManagerOutput.RelevantContext == ContextManagerAgent.NO_RELEVANT_CONTEXT_FOUND)
            {
                state.UserQuestionRelevantContext = null;
            }
            else
            {
                state.UserQuestionRelevantContext = contextManagerOutput.RelevantContext;
            }
            state.AddTokenUsage(ContextManagerAgentConfiguration.AgentName, contextManagerOutput.TokenCount, contextManagerOutput.InputTokenCount, contextManagerOutput.OutputTokenCount);

            _logger.LogInformation("Engaging Translator Agent...");

            var translatorOutput = await _translatorAgent.ExecuteAsync(new TranslatorAgentInput
            {
                Sentence = state.UserQuestion,
                TargetLanguage = INTERNAL_AGENTIC_LANGUAGE
            });
            state.TranslatorResponse = translatorOutput.TranslatedSentence;
            state.DetectedOriginalLanguage = translatorOutput.DetectedOriginalLanguage;
            state.AddTokenUsage(TranslatorAgentConfiguration.AgentName, translatorOutput.TokenCount, translatorOutput.InputTokenCount, translatorOutput.OutputTokenCount);

            if (!string.IsNullOrWhiteSpace(state.UserQuestionRelevantContext))
            {
                _logger.LogInformation("Engaging Context Aggregator Agent...");

                var contextAggregatorOutput = await _contextAggregatorAgent.ExecuteAsync(new ContextAggregatorAgentInput
                {
                    LastStatement = state.TranslatorResponse!,
                    ContextualInformation = state.UserQuestionRelevantContext ?? string.Empty
                });
                state.AggregatedUserQuestion = contextAggregatorOutput.AggregatedSentence;
                state.AddTokenUsage(ContextAggregatorAgentConfiguration.AgentName, contextAggregatorOutput.TokenCount, contextAggregatorOutput.InputTokenCount, contextAggregatorOutput.OutputTokenCount);
            }
            else
            {
                state.AggregatedUserQuestion = state.TranslatorResponse;
            }
            
            _logger.LogInformation("Engaging Router Agent...");
            var routerOutput = await _routerAgent.ExecuteAsync(new RouterAgentInput
            {
                Message = state.AggregatedUserQuestion!
            });
            state.RouterRecipient = routerOutput.Recipient;
            state.AddTokenUsage(RouterAgentConfiguration.AgentName, routerOutput.TokenCount, routerOutput.InputTokenCount, routerOutput.OutputTokenCount);

            if (routerOutput.Recipient?.Equals("PersonalAssistant", StringComparison.OrdinalIgnoreCase) == true)
            {
                await CompleteWorkflowAsync(state);
            }
            else if (routerOutput.Recipient?.Equals("BusinessRequirementsCreator", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Engaging Business Requirements Creator Agent...");
                var brcOutput = await _businessRequirementsCreatorAgent.ExecuteAsync(new BusinessRequirementsCreatorAgentInput
                {
                    UserQuestionText = state.AggregatedUserQuestion!
                });
                state.ShouldEngageCoder = brcOutput.EngageCoderAgent;
                state.AddTokenUsage(BusinessRequirementsCreatorAgentConfiguration.AgentName, brcOutput.TokenCount, brcOutput.InputTokenCount, brcOutput.OutputTokenCount);

                if (brcOutput.EngageCoderAgent)
                {
                    _logger.LogInformation("Engaging Coder Agent...");
                    state.BusinessRequirements = brcOutput.BusinessRequirements;

                    var coderAgentOutput = await _coderAgent.ExecuteAsync(new CoderAgentInput
                    {
                        BusinessRequirements = state.BusinessRequirements!
                    });
                    state.GeneratedCode = coderAgentOutput.CodeToRun;
                    state.AddTokenUsage(CoderAgentConfiguration.AgentName, coderAgentOutput.TokenCount, coderAgentOutput.InputTokenCount, coderAgentOutput.OutputTokenCount);

                    state.LastCodeWithLineNumbers = GetSourceCodeWithLineNumbers(state.GeneratedCode);
                    
                    _logger.LogInformation("Engaging Code Static Analyzer Agent...");
                    var staticAnalyzerOutput = await _codeStaticAnalyzer.ExecuteAsync(new CodeStaticAnalyzerInput
                    {
                        CodeToFix = state.LastCodeWithLineNumbers
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
                            CodeToFix = state.LastCodeWithLineNumbers,
                            Issues = state.CodeIssues
                        });
                        state.GeneratedCode = codeFixerOutput.FixedCode;
                        state.CodeFixerIterationCount++;
                        state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.TokenCount, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount);

                        state.LastCodeWithLineNumbers = GetSourceCodeWithLineNumbers(state.GeneratedCode);

                        var reAnalyzerOutput = await _codeStaticAnalyzer.ExecuteAsync(new CodeStaticAnalyzerInput
                        {
                            CodeToFix = state.LastCodeWithLineNumbers
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

                    var sandBoxError = false;
                    try
                    {
                        _logger.LogInformation("Engaging JS Sandbox Executor...");
                        var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new JSSandboxInput
                        {
                            Code = state.GeneratedCode
                        });
                        state.SandboxResult = executionOutput.Result;
                        state.SandboxError = null;
                    }
                    catch (Exception ex)
                    {
                        state.SandboxError = ex.Message;
                        state.SandboxResult = null;
                        sandBoxError = true;
                    }

                    _logger.LogInformation("Engaging Results Presenter Agent...");
                    var resultsPresenterOutput = await _resultsPresenterAgent.ExecuteAsync(new ResultsPresenterAgentInput
                    {
                        Content = sandBoxError ? state.SandboxError! : state.SandboxResult!
                    });
                    state.PresenterOutput = resultsPresenterOutput.Content;
                    state.AddTokenUsage(ResultsPresenterAgentConfiguration.AgentName, resultsPresenterOutput.TokenCount, resultsPresenterOutput.InputTokenCount, resultsPresenterOutput.OutputTokenCount);

                    await CompleteWorkflowAsync(state, state.PresenterOutput);
                }
                else
                {
                    state.OutputForUserFromBusinessAnalyst = brcOutput.AnswerToUserText;
                    await CompleteWorkflowAsync(state, state.OutputForUserFromBusinessAnalyst);
                }
            }
            else if (routerOutput.Recipient?.Equals("BusinessAdvisor", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Engaging Business Advisor Agent...");
                var baOutput = await _businessAdvisorAgent.ExecuteAsync(new BusinessAdvisorAgentInput
                {
                    UserQuestionText = state.AggregatedUserQuestion!
                });
                state.BusinessAdvisorContent = baOutput.Content;
                state.AddTokenUsage(BusinessAdvisorAgentConfiguration.AgentName, baOutput.TokenCount, baOutput.InputTokenCount, baOutput.OutputTokenCount);
                
                await CompleteWorkflowAsync(state, state.BusinessAdvisorContent);
            }
            else
            {
                throw new Exception($"Router Agent returned an unknown recipient: {routerOutput.Recipient}");
            }

            return new WorkflowResult
            {
                Response = state.FinalAnswer!,
                TokenUsageEntries = state.TokenUsageEntries
            };
        }

        private async Task CompleteWorkflowAsync(CodeModeWorkflowState state, string? data = null)
        {
            _logger.LogInformation("Engaging Personal Assistant Agent...");

            var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
            {
                Sentence = state.AggregatedUserQuestion!,
                Data = data,
                OutputLanguage = state.DetectedOriginalLanguage!
            });
            state.FinalAnswer = personalAssistantOutput.Response;
            state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount);

            _logger.LogInformation("Updating Context Manager Agent state with final answer...");
            var contextManagerState = await _contextManagerAgent.GetState();
            contextManagerState.ChatHistory.Add(new AgentMessage
            {
                Role = AgentMessageRole.Assistant,
                Content = state.FinalAnswer
            });
            await _contextManagerAgent.SetState(contextManagerState);
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
