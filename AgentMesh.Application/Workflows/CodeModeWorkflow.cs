using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Workflows
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
        private readonly ICodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent;
        private readonly IResultsPresenterAgent _resultsPresenterAgent;
        private readonly IJSSandboxExecutor _jsSandboxExecutor;
        private readonly IContextAnalyzerAgent _contextAnalyzerAgent;
        private readonly ITranslatorAgent _translatorAgent;
        private readonly IRouterAgent _routerAgent;
        private readonly IPersonalAssistantAgent _personalAssistantAgent;

        public CodeModeWorkflow(ILogger<CodeModeWorkflow> logger,
            IWorkflowProgressNotifier workflowProgressNotifier,
            IBusinessRequirementsCreatorAgent businessRequirementsCreatorAgent,
            IBusinessAdvisorAgent businessAdvisorAgent,
            ICoderAgent coderAgent,
            ICodeStaticAnalyzer codeStaticAnalyzer,
            ICodeFixerAgent codeFixerAgent,
            ICodeExecutionFailuresDetectorAgent codeExecutionFailuresDetectorAgent,
            IResultsPresenterAgent resultsPresenterAgent,
            IJSSandboxExecutor jsSandboxExecutor,
            IContextAnalyzerAgent contextAnalyzerAgent,
            ITranslatorAgent translatorAgent,
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
            _codeExecutionFailuresDetectorAgent = codeExecutionFailuresDetectorAgent;
            _resultsPresenterAgent = resultsPresenterAgent;
            _jsSandboxExecutor = jsSandboxExecutor;
            _contextAnalyzerAgent = contextAnalyzerAgent;
            _translatorAgent = translatorAgent;
            _routerAgent = routerAgent;
            _personalAssistantAgent = personalAssistantAgent;
        }

        public async Task<WorkflowResult> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory)
        {
            await _workflowProgressNotifier.NotifyWorkflowStart();

            var state = new CodeModeWorkflowState(userInput, chatHistory);

            _logger.LogDebug("Loading conversation history from Context Manager Agent...");

            await ExecuteContextAnalyzerAsync(state, chatHistory);
            await ExecuteTranslatorAsync(state);

            var routerRecipient = await ExecuteRouterAsync(state);

            if (routerRecipient?.Equals("PersonalAssistant", StringComparison.OrdinalIgnoreCase) == true)
            {
                goto CompleteWorkflow;
            }
            else if (routerRecipient?.Equals("BusinessRequirementsCreator", StringComparison.OrdinalIgnoreCase) == true)
            {
                await ExecuteBusinessRequirementsCreatorAsync(state);
                await ExecuteCoderAsync(state);
                await ExecuteCodeStaticAnalyzerAsync(state);

                for (int i = 0; i < 2 && !state.IsCodeValid && state.CodeIssues.Any(); i++)
                {
                    await ExecuteCodeFixerAsync(state, i + 1, false);
                    await ExecuteCodeStaticAnalyzerAsync(state);
                }

                bool sandBoxError = await ExecuteJSSandboxAsync(state, false);

                for (int i = 0; i < 2 && state.CodeExecutionFailuresDetectorIterationCount < 2; i++)
                {
                    var analysis = await ExecuteCodeExecutionFailuresDetectorAsync(state, i + 1);

                    if (analysis.Equals(JavascriptCodeExecutionFailuresDetectorAgent.NO_ERROR, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    await ExecuteCodeFixerForRuntimeErrorsAsync(state, analysis, i + 1);

                    sandBoxError = await ExecuteJSSandboxAsync(state, true);
                    if (sandBoxError)
                    {
                        break;
                    }
                }

                await ExecuteResultsPresenterAsync(state, sandBoxError);

                await CompleteWorkflowAsync(state, state.PresenterOutput);
                goto WorkflowEnd;
            }
            else if (routerRecipient?.Equals("BusinessAdvisor", StringComparison.OrdinalIgnoreCase) == true)
            {
                await ExecuteBusinessAdvisorAsync(state);
                await CompleteWorkflowAsync(state, state.BusinessAdvisorContent);
                goto WorkflowEnd;
            }
            else
            {
                throw new Exception($"Router Agent returned an unknown recipient: {routerRecipient}");
            }

        CompleteWorkflow:
            await CompleteWorkflowAsync(state);

        WorkflowEnd:
            await _workflowProgressNotifier.NotifyWorkflowEnd();

            return new WorkflowResult
            {
                Response = state.FinalAnswer!,
                TokenUsageEntries = state.TokenUsageEntries
            };
        }

        private async Task ExecuteContextAnalyzerAsync(CodeModeWorkflowState state, IEnumerable<ContextMessage> chatHistory)
        {
            _logger.LogDebug("Engaging Context Analyzer Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Context Analyzer Agent", new Dictionary<string, string>
            {
                { "ContextMessages", "<omitted for brevity>. Total: " + chatHistory.Count().ToString() },
                { "UserLastRequest", state.OriginalUserRequest }
            });

            var contextAnalyzerOutput = await _contextAnalyzerAgent.ExecuteAsync(new ContextAnalyzerAgentInput
            {
                ContextMessages = state.InitialContextMessages.ToList(),
                UserLastRequest = state.OriginalUserRequest
            });
            if (contextAnalyzerOutput.RelevantContext == ContextAnalyzerAgent.NO_RELEVANT_CONTEXT_FOUND)
            {
                state.UserQuestionRelevantContext = null;
            }
            else
            {
                state.UserQuestionRelevantContext = contextAnalyzerOutput.RelevantContext;
            }
            state.AddTokenUsage(ContextAnalyzerAgentConfiguration.AgentName, contextAnalyzerOutput.TokenCount, contextAnalyzerOutput.InputTokenCount, contextAnalyzerOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Context Analyzer Agent", new Dictionary<string, string>
            {
                { "RelevantContext", state.UserQuestionRelevantContext ?? "(No relevant context found)" }
            });
        }

        private async Task ExecuteTranslatorAsync(CodeModeWorkflowState state)
        {
            _logger.LogDebug("Engaging Translator Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Translator Agent", new Dictionary<string, string>
            {
                { "TargetLanguage", INTERNAL_AGENTIC_LANGUAGE },
                { "UserRequest", state.OriginalUserRequest },
                { "RequestContext", state.UserQuestionRelevantContext ?? string.Empty }
            });

            var translatorOutput = await _translatorAgent.ExecuteAsync(new TranslatorAgentInput
            {
                TargetLanguage = INTERNAL_AGENTIC_LANGUAGE,
                UserRequest = state.OriginalUserRequest,
                RequestContext = state.UserQuestionRelevantContext ?? string.Empty
            });
            state.EnglishTranslatedUserRequest = translatorOutput.TranslatedSentence;
            state.TranslatedContext = translatorOutput.TranslatedContext;
            state.DetectedOriginalLanguage = translatorOutput.DetectedOriginalLanguage;
            state.AddTokenUsage(TranslatorAgentConfiguration.AgentName, translatorOutput.TokenCount, translatorOutput.InputTokenCount, translatorOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Translator Agent", new Dictionary<string, string>
            {
                { "TranslatedSentence", state.EnglishTranslatedUserRequest! },
                { "TranslatedContext", state.TranslatedContext ?? "(No context translated)" },
                { "DetectedOriginalLanguage", state.DetectedOriginalLanguage! }
            });
        }

        private async Task<string?> ExecuteRouterAsync(CodeModeWorkflowState state)
        {
            _logger.LogDebug("Engaging Router Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Router Agent", new Dictionary<string, string>
            {
                { "UserRequest", state.EnglishTranslatedUserRequest },
                { "RequestContext", state.TranslatedContext ?? string.Empty }
            });

            var routerOutput = await _routerAgent.ExecuteAsync(new RouterAgentInput
            {
                UserRequest = state.EnglishTranslatedUserRequest,
                RequestContext = state.TranslatedContext ?? string.Empty
            });
            state.RouterRecipient = routerOutput.Recipient;
            state.AddTokenUsage(RouterAgentConfiguration.AgentName, routerOutput.TokenCount, routerOutput.InputTokenCount, routerOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Router Agent", new Dictionary<string, string>
            {
                { "Recipient", routerOutput.Recipient ?? "(Unknown)" },
                { "Rationale", routerOutput.Rationale ?? "(No rationale provided)" }
            });

            return routerOutput.Recipient;
        }

        private async Task ExecuteBusinessRequirementsCreatorAsync(CodeModeWorkflowState state)
        {
            _logger.LogDebug("Engaging Business Requirements Creator Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Business Requirements Creator Agent", new Dictionary<string, string>
            {
                { "UserRequest", state.EnglishTranslatedUserRequest },
                { "RequestContext", state.TranslatedContext ?? string.Empty }
            });

            var brcOutput = await _businessRequirementsCreatorAgent.ExecuteAsync(new BusinessRequirementsCreatorAgentInput
            {
                UserRequest = state.EnglishTranslatedUserRequest,
                RequestContext = state.TranslatedContext ?? string.Empty
            });
            state.ShouldEngageCoder = true;
            state.BusinessRequirements = brcOutput.BusinessRequirements;
            state.AddTokenUsage(BusinessRequirementsCreatorAgentConfiguration.AgentName, brcOutput.TokenCount, brcOutput.InputTokenCount, brcOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Business Requirements Creator Agent", new Dictionary<string, string>
            {
                { "BusinessRequirements", brcOutput.BusinessRequirements }
            });
        }

        private async Task ExecuteCoderAsync(CodeModeWorkflowState state)
        {
            _logger.LogDebug("Engaging Coder Agent...");
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
        }

        private async Task ExecuteCodeStaticAnalyzerAsync(CodeModeWorkflowState state)
        {
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
            else
            {
                state.CodeIssues.Clear();
            }
            state.AddTokenUsage(CodeStaticAnalyzerConfiguration.AgentName, staticAnalyzerOutput.TokenCount, staticAnalyzerOutput.InputTokenCount, staticAnalyzerOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Code Static Analyzer Agent", new Dictionary<string, string>
            {
                { "IsCodeValid", state.IsCodeValid.ToString() },
                { "ViolationsCount", staticAnalyzerOutput.Violations.Count().ToString() }
            });
        }

        private async Task ExecuteCodeFixerAsync(CodeModeWorkflowState state, int iteration, bool isRuntimeFix)
        {
            var agentName = isRuntimeFix ? $"Code Fixer Agent for Runtime Errors (Iteration {iteration})" : $"Code Fixer Agent (Iteration {iteration})";
            
            _logger.LogDebug("Engaging Code Fixer Agent... Iteration {Iteration}", iteration);
            await _workflowProgressNotifier.NotifyWorkflowStepStart(agentName, new Dictionary<string, string>
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
            await _workflowProgressNotifier.NotifyWorkflowStepEnd(agentName, new Dictionary<string, string>
            {
                { "FixedCode", state.GeneratedCode }
            });
        }

        private async Task<bool> ExecuteJSSandboxAsync(CodeModeWorkflowState state, bool isReexecution)
        {
            var stepName = isReexecution ? "JS Sandbox Executor (Re-execution)" : "JS Sandbox Executor";
            var logMessage = isReexecution ? "Re-executing JS Sandbox Executor after runtime fix..." : "Engaging JS Sandbox Executor...";

            bool sandBoxError = false;
            try
            {
                _logger.LogDebug(logMessage);
                await _workflowProgressNotifier.NotifyWorkflowStepStart(stepName, new Dictionary<string, string>
                {
                    { "Code", state.GeneratedCode }
                });

                var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new JSSandboxInput
                {
                    Code = state.GeneratedCode
                });
                state.SandboxResult = executionOutput.Result;
                state.SandboxError = null;
                await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, new Dictionary<string, string>
                {
                    { "Result", state.SandboxResult }
                });
            }
            catch (Exception ex)
            {
                state.SandboxError = ex.Message;
                state.SandboxResult = null;
                sandBoxError = true;
                await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, new Dictionary<string, string>
                {
                    { "Error", state.SandboxError }
                });
            }

            return sandBoxError;
        }

        private async Task<string> ExecuteCodeExecutionFailuresDetectorAsync(CodeModeWorkflowState state, int iteration)
        {
            _logger.LogDebug("Engaging Code Execution Failures Detector Agent... Iteration {Iteration}", iteration);
            await _workflowProgressNotifier.NotifyWorkflowStepStart($"Code Execution Failures Detector Agent (Iteration {iteration})", new Dictionary<string, string>
            {
                { "CodeWithLineNumbers", state.LastCodeWithLineNumbers },
                { "ExecutionResult", state.SandboxResult! }
            });

            var detectorOutput = await _codeExecutionFailuresDetectorAgent.ExecuteAsync(new CodeExecutionFailuresDetectorAgentInput
            {
                CodeWithLineNumbers = state.LastCodeWithLineNumbers,
                ExecutionResult = state.SandboxResult!
            });
            state.CodeExecutionFailuresDetectorIterationCount++;
            state.AddTokenUsage(CodeExecutionFailuresDetectorAgentConfiguration.AgentName, detectorOutput.TokenCount, detectorOutput.InputTokenCount, detectorOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd($"Code Execution Failures Detector Agent (Iteration {iteration})", new Dictionary<string, string>
            {
                { "Analysis", detectorOutput.Analysis }
            });

            return detectorOutput.Analysis;
        }

        private async Task ExecuteCodeFixerForRuntimeErrorsAsync(CodeModeWorkflowState state, string analysis, int iteration)
        {
            _logger.LogDebug("Engaging Code Fixer Agent for runtime errors... Iteration {Iteration}", iteration);
            await _workflowProgressNotifier.NotifyWorkflowStepStart($"Code Fixer Agent for Runtime Errors (Iteration {iteration})", new Dictionary<string, string>
            {
                { "CodeToFix", state.LastCodeWithLineNumbers },
                { "IssuesCount", "1" }
            });

            var codeFixerOutput = await _codeFixerAgent.ExecuteAsync(new CodeFixerAgentInput
            {
                CodeToFix = state.LastCodeWithLineNumbers,
                Issues = new[] { analysis }
            });
            state.GeneratedCode = codeFixerOutput.FixedCode;
            state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.TokenCount, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd($"Code Fixer Agent for Runtime Errors (Iteration {iteration})", new Dictionary<string, string>
            {
                { "FixedCode", state.GeneratedCode }
            });
        }

        private async Task ExecuteResultsPresenterAsync(CodeModeWorkflowState state, bool sandBoxError)
        {
            _logger.LogDebug("Engaging Results Presenter Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Results Presenter Agent", new Dictionary<string, string>
            {
                { "Data", sandBoxError ? state.SandboxError! : state.SandboxResult! },
                { "UserRequest", state.EnglishTranslatedUserRequest },
                { "RequestContext", state.TranslatedContext ?? string.Empty }
            });

            var resultsPresenterOutput = await _resultsPresenterAgent.ExecuteAsync(new ResultsPresenterAgentInput
            {
                Data = sandBoxError ? state.SandboxError! : state.SandboxResult!,
                UserRequest = state.EnglishTranslatedUserRequest,
                RequestContext = state.TranslatedContext ?? string.Empty
            });
            state.PresenterOutput = resultsPresenterOutput.Content;
            state.AddTokenUsage(ResultsPresenterAgentConfiguration.AgentName, resultsPresenterOutput.TokenCount, resultsPresenterOutput.InputTokenCount, resultsPresenterOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Results Presenter Agent", new Dictionary<string, string>
            {
                { "Content", state.PresenterOutput }
            });
        }

        private async Task ExecuteBusinessAdvisorAsync(CodeModeWorkflowState state)
        {
            _logger.LogDebug("Engaging Business Advisor Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Business Advisor Agent", new Dictionary<string, string>
            {
                { "UserRequest", state.EnglishTranslatedUserRequest },
                { "RequestContext", state.TranslatedContext ?? string.Empty }
            });

            var baOutput = await _businessAdvisorAgent.ExecuteAsync(new BusinessAdvisorAgentInput
            {
                UserRequest = state.EnglishTranslatedUserRequest,
                RequestContext = state.TranslatedContext ?? string.Empty
            });
            state.BusinessAdvisorContent = baOutput.Content;
            state.AddTokenUsage(BusinessAdvisorAgentConfiguration.AgentName, baOutput.TokenCount, baOutput.InputTokenCount, baOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Business Advisor Agent", new Dictionary<string, string>
            {
                { "Content", state.BusinessAdvisorContent }
            });
        }

        private async Task CompleteWorkflowAsync(CodeModeWorkflowState state, string? data = null)
        {
            _logger.LogDebug("Engaging Personal Assistant Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Personal Assistant Agent", new Dictionary<string, string>
            {
                { "Data", data ?? "(No data)" },
                { "OutputLanguage", state.DetectedOriginalLanguage! },
                { "UserRequest", state.EnglishTranslatedUserRequest! },
                { "RequestContext", state.TranslatedContext ?? string.Empty }
            });

            var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
            {
                Data = data,
                OutputLanguage = state.DetectedOriginalLanguage!,
                UserRequest = state.EnglishTranslatedUserRequest,
                RequestContext = state.TranslatedContext ?? string.Empty
            });
            state.FinalAnswer = personalAssistantOutput.Response;
            state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Personal Assistant Agent", new Dictionary<string, string>
            {
                { "Response", state.FinalAnswer }
            });
        }


        public string GetIngressAgentName() => ContextAnalyzerAgentConfiguration.AgentName;

        public string GetEgressAgentName() => PersonalAssistantAgentConfiguration.AgentName;
    }
}
