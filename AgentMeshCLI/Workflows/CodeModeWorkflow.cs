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
        //private readonly IContextManagerAgent _contextManagerAgent;
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
            IResultsPresenterAgent resultsPresenterAgent,
            IJSSandboxExecutor jsSandboxExecutor,
            IContextManagerAgent contextManagerAgent,
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
            _resultsPresenterAgent = resultsPresenterAgent;
            _jsSandboxExecutor = jsSandboxExecutor;
            //_contextManagerAgent = contextManagerAgent;
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
            state.TranslatorResponse = translatorOutput.TranslatedSentence;
            state.TranslatedContext = translatorOutput.TranslatedContext;
            state.DetectedOriginalLanguage = translatorOutput.DetectedOriginalLanguage;
            state.AddTokenUsage(TranslatorAgentConfiguration.AgentName, translatorOutput.TokenCount, translatorOutput.InputTokenCount, translatorOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Translator Agent", new Dictionary<string, string>
            {
                { "TranslatedSentence", state.TranslatorResponse! },
                { "TranslatedContext", state.TranslatedContext ?? "(No context translated)" },
                { "DetectedOriginalLanguage", state.DetectedOriginalLanguage! }
            });

            state.EnglishTranslatedUserRequest = state.TranslatorResponse;
            
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

            if (routerOutput.Recipient?.Equals("PersonalAssistant", StringComparison.OrdinalIgnoreCase) == true)
            {
                await CompleteWorkflowAsync(state);
            }
            else if (routerOutput.Recipient?.Equals("BusinessRequirementsCreator", StringComparison.OrdinalIgnoreCase) == true)
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

                await CompleteWorkflowAsync(state, state.PresenterOutput);             
            }
            else if (routerOutput.Recipient?.Equals("BusinessAdvisor", StringComparison.OrdinalIgnoreCase) == true)
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
