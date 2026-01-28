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
        private readonly IWorkflowProgressNotifier _workflowProgressNotifier;

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
            IWorkflowProgressNotifier workflowProgressNotifier,
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
            _workflowProgressNotifier = workflowProgressNotifier;
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
            await _workflowProgressNotifier.NotifyWorkflowStart();

            var state = new CodeModeWorkflowState(userInput);

            _logger.LogDebug("Engaging Context Manager Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Context Manager Agent", new Dictionary<string, string>
            {
                { "UserSentenceText", state.UserQuestion }
            });

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
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Context Manager Agent", new Dictionary<string, string>
            {
                { "RelevantContext", state.UserQuestionRelevantContext ?? "(No relevant context found)" }
            });

            _logger.LogDebug("Engaging Translator Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Translator Agent", new Dictionary<string, string>
            {
                { "Sentence", state.UserQuestion },
                { "TargetLanguage", INTERNAL_AGENTIC_LANGUAGE }
            });

            var translatorOutput = await _translatorAgent.ExecuteAsync(new TranslatorAgentInput
            {
                Sentence = state.UserQuestion,
                TargetLanguage = INTERNAL_AGENTIC_LANGUAGE
            });
            state.TranslatorResponse = translatorOutput.TranslatedSentence;
            state.DetectedOriginalLanguage = translatorOutput.DetectedOriginalLanguage;
            state.AddTokenUsage(TranslatorAgentConfiguration.AgentName, translatorOutput.TokenCount, translatorOutput.InputTokenCount, translatorOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Translator Agent", new Dictionary<string, string>
            {
                { "TranslatedSentence", state.TranslatorResponse! },
                { "DetectedOriginalLanguage", state.DetectedOriginalLanguage! }
            });

            if (!string.IsNullOrWhiteSpace(state.UserQuestionRelevantContext))
            {
                _logger.LogDebug("Engaging Context Aggregator Agent...");
                await _workflowProgressNotifier.NotifyWorkflowStepStart("Context Aggregator Agent", new Dictionary<string, string>
                {
                    { "LastStatement", state.TranslatorResponse! },
                    { "ContextualInformation", state.UserQuestionRelevantContext ?? string.Empty }
                });

                var contextAggregatorOutput = await _contextAggregatorAgent.ExecuteAsync(new ContextAggregatorAgentInput
                {
                    LastStatement = state.TranslatorResponse!,
                    ContextualInformation = state.UserQuestionRelevantContext ?? string.Empty
                });
                state.AggregatedUserQuestion = contextAggregatorOutput.AggregatedSentence;
                state.AddTokenUsage(ContextAggregatorAgentConfiguration.AgentName, contextAggregatorOutput.TokenCount, contextAggregatorOutput.InputTokenCount, contextAggregatorOutput.OutputTokenCount);
                await _workflowProgressNotifier.NotifyWorkflowStepEnd("Context Aggregator Agent", new Dictionary<string, string>
                {
                    { "AggregatedSentence", state.AggregatedUserQuestion! }
                });
            }
            else
            {
                state.AggregatedUserQuestion = state.TranslatorResponse;
            }
            
            _logger.LogDebug("Engaging Router Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Router Agent", new Dictionary<string, string>
            {
                { "Message", state.AggregatedUserQuestion! }
            });

            var routerOutput = await _routerAgent.ExecuteAsync(new RouterAgentInput
            {
                Message = state.AggregatedUserQuestion!
            });
            state.RouterRecipient = routerOutput.Recipient;
            state.AddTokenUsage(RouterAgentConfiguration.AgentName, routerOutput.TokenCount, routerOutput.InputTokenCount, routerOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Router Agent", new Dictionary<string, string>
            {
                { "Recipient", routerOutput.Recipient ?? "(Unknown)" }
            });

            if (routerOutput.Recipient?.Equals("PersonalAssistant", StringComparison.OrdinalIgnoreCase) == true)
            {
                await CompleteWorkflowAsync(state);
            }
            else if (routerOutput.Recipient?.Equals("BusinessRequirementsCreator", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogDebug("Engaging Business Requirements Creator Agent...");
                await _workflowProgressNotifier.NotifyWorkflowStepStart("Business Requirements Creator Agent", new Dictionary<string, string>
                {
                    { "UserQuestionText", state.AggregatedUserQuestion! }
                });

                var brcOutput = await _businessRequirementsCreatorAgent.ExecuteAsync(new BusinessRequirementsCreatorAgentInput
                {
                    UserQuestionText = state.AggregatedUserQuestion!
                });
                state.ShouldEngageCoder = true;
                state.AddTokenUsage(BusinessRequirementsCreatorAgentConfiguration.AgentName, brcOutput.TokenCount, brcOutput.InputTokenCount, brcOutput.OutputTokenCount);
                await _workflowProgressNotifier.NotifyWorkflowStepEnd("Business Requirements Creator Agent", new Dictionary<string, string>
                {
                    { "BusinessRequirements", brcOutput.BusinessRequirements }
                });

                _logger.LogDebug("Engaging Coder Agent...");
                state.BusinessRequirements = brcOutput.BusinessRequirements;
                await _workflowProgressNotifier.NotifyWorkflowStepStart("Coder Agent", new Dictionary<string, string>
                {
                    { "BusinessRequirements", state.BusinessRequirements! }
                });

                var coderAgentOutput = await _coderAgent.ExecuteAsync(new CoderAgentInput
                {
                    BusinessRequirements = state.BusinessRequirements!
                });
                state.GeneratedCode = coderAgentOutput.CodeToRun;
                state.AddTokenUsage(CoderAgentConfiguration.AgentName, coderAgentOutput.TokenCount, coderAgentOutput.InputTokenCount, coderAgentOutput.OutputTokenCount);
                await _workflowProgressNotifier.NotifyWorkflowStepEnd("Coder Agent", new Dictionary<string, string>
                {
                    { "CodeToRun", state.GeneratedCode }
                });

                state.LastCodeWithLineNumbers = GetSourceCodeWithLineNumbers(state.GeneratedCode);
                    
                _logger.LogDebug("Engaging Code Static Analyzer Agent...");
                await _workflowProgressNotifier.NotifyWorkflowStepStart("Code Static Analyzer Agent", new Dictionary<string, string>
                {
                    { "CodeToFix", state.LastCodeWithLineNumbers }
                });

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
                await _workflowProgressNotifier.NotifyWorkflowStepEnd("Code Static Analyzer Agent", new Dictionary<string, string>
                {
                    { "IsCodeValid", state.IsCodeValid.ToString() },
                    { "ViolationsCount", staticAnalyzerOutput.Violations.Count().ToString() }
                });

                for (int i = 0; i < 2 && !state.IsCodeValid && state.CodeIssues.Any(); i++)
                {
                    _logger.LogDebug("Engaging Code Fixer Agent... Iteration {Iteration}", i + 1);
                    await _workflowProgressNotifier.NotifyWorkflowStepStart($"Code Fixer Agent (Iteration {i + 1})", new Dictionary<string, string>
                    {
                        { "CodeToFix", state.LastCodeWithLineNumbers },
                        { "IssuesCount", state.CodeIssues.Count.ToString() }
                    });

                    var codeFixerOutput = await _codeFixerAgent.ExecuteAsync(new CodeFixerAgentInput
                    {
                        CodeToFix = state.LastCodeWithLineNumbers,
                        Issues = state.CodeIssues
                    });
                    state.GeneratedCode = codeFixerOutput.FixedCode;
                    state.CodeFixerIterationCount++;
                    state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.TokenCount, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount);
                    await _workflowProgressNotifier.NotifyWorkflowStepEnd($"Code Fixer Agent (Iteration {i + 1})", new Dictionary<string, string>
                    {
                        { "FixedCode", state.GeneratedCode }
                    });

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
                    _logger.LogDebug("Engaging JS Sandbox Executor...");
                    await _workflowProgressNotifier.NotifyWorkflowStepStart("JS Sandbox Executor", new Dictionary<string, string>
                    {
                        { "Code", state.GeneratedCode }
                    });

                    var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new JSSandboxInput
                    {
                        Code = state.GeneratedCode
                    });
                    state.SandboxResult = executionOutput.Result;
                    state.SandboxError = null;
                    await _workflowProgressNotifier.NotifyWorkflowStepEnd("JS Sandbox Executor", new Dictionary<string, string>
                    {
                        { "Result", state.SandboxResult }
                    });
                }
                catch (Exception ex)
                {
                    state.SandboxError = ex.Message;
                    state.SandboxResult = null;
                    sandBoxError = true;
                    await _workflowProgressNotifier.NotifyWorkflowStepEnd("JS Sandbox Executor", new Dictionary<string, string>
                    {
                        { "Error", state.SandboxError }
                    });
                }

                _logger.LogDebug("Engaging Results Presenter Agent...");
                await _workflowProgressNotifier.NotifyWorkflowStepStart("Results Presenter Agent", new Dictionary<string, string>
                {
                    { "Content", sandBoxError ? state.SandboxError! : state.SandboxResult! }
                });

                var resultsPresenterOutput = await _resultsPresenterAgent.ExecuteAsync(new ResultsPresenterAgentInput
                {
                    Content = sandBoxError ? state.SandboxError! : state.SandboxResult!
                });
                state.PresenterOutput = resultsPresenterOutput.Content;
                state.AddTokenUsage(ResultsPresenterAgentConfiguration.AgentName, resultsPresenterOutput.TokenCount, resultsPresenterOutput.InputTokenCount, resultsPresenterOutput.OutputTokenCount);
                await _workflowProgressNotifier.NotifyWorkflowStepEnd("Results Presenter Agent", new Dictionary<string, string>
                {
                    { "Content", state.PresenterOutput }
                });

                await CompleteWorkflowAsync(state, state.PresenterOutput);             
            }
            else if (routerOutput.Recipient?.Equals("BusinessAdvisor", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogDebug("Engaging Business Advisor Agent...");
                await _workflowProgressNotifier.NotifyWorkflowStepStart("Business Advisor Agent", new Dictionary<string, string>
                {
                    { "UserQuestionText", state.AggregatedUserQuestion! }
                });

                var baOutput = await _businessAdvisorAgent.ExecuteAsync(new BusinessAdvisorAgentInput
                {
                    UserQuestionText = state.AggregatedUserQuestion!
                });
                state.BusinessAdvisorContent = baOutput.Content;
                state.AddTokenUsage(BusinessAdvisorAgentConfiguration.AgentName, baOutput.TokenCount, baOutput.InputTokenCount, baOutput.OutputTokenCount);
                await _workflowProgressNotifier.NotifyWorkflowStepEnd("Business Advisor Agent", new Dictionary<string, string>
                {
                    { "Content", state.BusinessAdvisorContent }
                });
                
                await CompleteWorkflowAsync(state, state.BusinessAdvisorContent);
            }
            else
            {
                throw new Exception($"Router Agent returned an unknown recipient: {routerOutput.Recipient}");
            }

            await _workflowProgressNotifier.NotifyWorkflowEnd();

            return new WorkflowResult
            {
                Response = state.FinalAnswer!,
                TokenUsageEntries = state.TokenUsageEntries
            };
        }

        private async Task CompleteWorkflowAsync(CodeModeWorkflowState state, string? data = null)
        {
            _logger.LogDebug("Engaging Personal Assistant Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Personal Assistant Agent", new Dictionary<string, string>
            {
                { "Sentence", state.AggregatedUserQuestion! },
                { "Data", data ?? "(No data)" },
                { "OutputLanguage", state.DetectedOriginalLanguage! }
            });

            var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
            {
                Sentence = state.AggregatedUserQuestion!,
                Data = data,
                OutputLanguage = state.DetectedOriginalLanguage!
            });
            state.FinalAnswer = personalAssistantOutput.Response;
            state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Personal Assistant Agent", new Dictionary<string, string>
            {
                { "Response", state.FinalAnswer }
            });

            _logger.LogDebug("Updating Context Manager Agent state with final answer...");
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
