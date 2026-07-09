using AgentMesh.Models.IntentExtractor;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Models;
using AgentMesh.Models.PersonalAssistant;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Application.Workflows.Steps;

namespace AgentMesh.Application.Workflows
{
    public partial class CodeModeWorkflow(
        ILogger<CodeModeWorkflow> logger,
        IWorkflowProgressNotifier workflowProgressNotifier,
        IPersonalAssistantAgent personalAssistantAgent,
        CodeModeWorkflowConfiguration workflowConfiguration,
        IntentExtractorWorkflowStep intentExtractorWorkflowStep,
        DomainsKnowledgeBaseServiceFastSearchWorkflowStep domainsKnowledgeBaseServiceFastSearchWorkflowStep,
        RequirementsCollectorWorkflowStep requirementsCollectorWorkflowStep,
        AgentMemoryServiceWorkflowStep agentMemoryServiceWorkflowStep,
        DomainsKnowledgeBaseServiceSearchWorkflowStep domainsKnowledgeBaseServiceSearchWorkflowStep,
        DomainsKnowledgeBaseDocumentsExtractorWorkflowStep domainsKnowledgeBaseDocumentsExtractorWorkflowStep,
        IntentCanonicalizationWorkflowStep intentCanonicalizationWorkflowStep,
        DocumentationWorkflowStep documentationWorkflowStep,
        FunctionalAnalystWorkflowStep functionalAnalystWorkflowStep,
        APIsKnowledgeBaseServiceSearchWorkflowStep apisKnowledgeBaseServiceSearchWorkflowStep,
        APIKnowledgeBaseDocumentsExtractorWorkflowStep apiKnowledgeBaseDocumentsExtractorWorkflowStep,
        TechnicalAnalystWorkflowStep technicalAnalystWorkflowStep,
        CoderWorkflowStep coderWorkflowStep,
        JSSandboxWorkflowStep jsSandboxWorkflowStep,
        CodeExecutionFailuresDetectorWorkflowStep codeExecutionFailuresDetectorWorkflowStep,
        CodeFixerForRuntimeErrorsWorkflowStep codeFixerForRuntimeErrorsWorkflowStep,
        DomainExpertWorkflowStep domainExpertWorkflowStep) : IWorkflow
    {
        private readonly ILogger<CodeModeWorkflow> _logger = logger;
        private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
        private readonly IPersonalAssistantAgent _personalAssistantAgent = personalAssistantAgent;
        private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;

        private readonly IntentExtractorWorkflowStep _intentExtractorWorkflowStep = intentExtractorWorkflowStep;
        private readonly DomainsKnowledgeBaseServiceFastSearchWorkflowStep _domainsKnowledgeBaseServiceFastSearchWorkflowStep = domainsKnowledgeBaseServiceFastSearchWorkflowStep;
        private readonly RequirementsCollectorWorkflowStep _requirementsCollectorWorkflowStep = requirementsCollectorWorkflowStep;
        private readonly AgentMemoryServiceWorkflowStep _agentMemoryServiceWorkflowStep = agentMemoryServiceWorkflowStep;
        private readonly DomainsKnowledgeBaseServiceSearchWorkflowStep _domainsKnowledgeBaseServiceSearchWorkflowStep = domainsKnowledgeBaseServiceSearchWorkflowStep;
        private readonly DomainsKnowledgeBaseDocumentsExtractorWorkflowStep _domainsKnowledgeBaseDocumentsExtractorWorkflowStep = domainsKnowledgeBaseDocumentsExtractorWorkflowStep;
        private readonly IntentCanonicalizationWorkflowStep _intentCanonicalizationWorkflowStep = intentCanonicalizationWorkflowStep;
        private readonly DocumentationWorkflowStep _documentationWorkflowStep = documentationWorkflowStep;
        private readonly FunctionalAnalystWorkflowStep _functionalAnalystWorkflowStep = functionalAnalystWorkflowStep;
        private readonly APIsKnowledgeBaseServiceSearchWorkflowStep _apisKnowledgeBaseServiceSearchWorkflowStep = apisKnowledgeBaseServiceSearchWorkflowStep;
        private readonly APIKnowledgeBaseDocumentsExtractorWorkflowStep _apiKnowledgeBaseDocumentsExtractorWorkflowStep = apiKnowledgeBaseDocumentsExtractorWorkflowStep;
        private readonly TechnicalAnalystWorkflowStep _technicalAnalystWorkflowStep = technicalAnalystWorkflowStep;
        private readonly CoderWorkflowStep _coderWorkflowStep = coderWorkflowStep;
        private readonly JSSandboxWorkflowStep _jsSandboxWorkflowStep = jsSandboxWorkflowStep;
        private readonly CodeExecutionFailuresDetectorWorkflowStep _codeExecutionFailuresDetectorWorkflowStep = codeExecutionFailuresDetectorWorkflowStep;
        private readonly CodeFixerForRuntimeErrorsWorkflowStep _codeFixerForRuntimeErrorsWorkflowStep = codeFixerForRuntimeErrorsWorkflowStep;
        private readonly DomainExpertWorkflowStep _domainExpertWorkflowStep = domainExpertWorkflowStep;

        public async Task<WorkflowResult> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory)
        {
            await _workflowProgressNotifier.NotifyWorkflowStart();

            var state = new CodeModeWorkflowState(userInput, chatHistory);

            await _intentExtractorWorkflowStep.ExecuteIntentExtractorAsync(state, chatHistory);

            if (state.ClassifiedUserRequest.IntentCategory == UserIntentCategoryValues.Other)
            {
                goto CompleteWorkflow;
            }

            if (state.ClassifiedUserRequest.EntitiesByDomain.Any())
            {
                await _domainsKnowledgeBaseServiceFastSearchWorkflowStep.ExecuteDomainsKnowledgeBaseServiceFastSearchAsync(state);
            }

            await _requirementsCollectorWorkflowStep.ExecuteRequirementsCollectorAsync(state);

            var memoryTask = (_workflowConfiguration.EnableMemoryService && state.PastMemoriesQuery.Any())
                ? _agentMemoryServiceWorkflowStep.ExecuteAgentMemoryServiceAsync(state)
                : Task.CompletedTask;

            var knowledgeBaseTask = state.DomainsKnowledgeBaseQuery.Any()
                ? _domainsKnowledgeBaseServiceSearchWorkflowStep.ExecuteDomainsKnowledgeBaseServiceSearchAsync(state)
                : Task.CompletedTask;

            await Task.WhenAll(memoryTask, knowledgeBaseTask);

            if (state.DomainsKnowledgeBaseQueryResults.Results.Any())
            {
                await _domainsKnowledgeBaseDocumentsExtractorWorkflowStep.ExecuteDomainsKnowledgeBaseDocumentsExtractorAsync(state);
            }

            await _intentCanonicalizationWorkflowStep.ExecuteIntentCanonicalizationAsync(state);

            if (state.ClassifiedUserRequest.CanonicalizedIntentCategory == UserIntentCategoryValues.Documentation)
            {
                await _documentationWorkflowStep.ExecuteDocumentationAgentAsync(state);
            }
            else if (state.ClassifiedUserRequest.CanonicalizedIntentCategory == UserIntentCategoryValues.TaskExecution)
            {
                var functionAnalystTask = _functionalAnalystWorkflowStep.ExecuteFunctionalAnalystAsync(state);
                var apisKnowledgeBaseServiceSearchTask = _apisKnowledgeBaseServiceSearchWorkflowStep.ExecuteAPIsKnowledgeBaseServiceSearchAsync(state);

                await Task.WhenAll(functionAnalystTask, apisKnowledgeBaseServiceSearchTask);

                if (state.FunctionalAnalystRejected)
                {
                    goto CompleteWorkflow;
                }

                if (state.APISKnowledgeBaseQueryResults.Results.Any())
                {
                    await _apiKnowledgeBaseDocumentsExtractorWorkflowStep.ExecuteAPIKnowledgeBaseDocumentsExtractorAsync(state);
                }

                await _technicalAnalystWorkflowStep.ExecuteTechnicalAnalystAsync(state);

                if (state.TechnicalAnalystRejected)
                {
                    goto CompleteWorkflow;
                }

                await _coderWorkflowStep.ExecuteCoderAsync(state);

                await _jsSandboxWorkflowStep.ExecuteJSSandboxAsync(state, false);

                if (state.CodeExecutionResultType == SandboxResultType.CallError)
                {
                    await CompleteWorkflowAsync(state);
                }
                else if (_workflowConfiguration.EnableCodeCorrection &&
                    (state.CodeExecutionResultType == SandboxResultType.ApplicationError ||
                         state.CodeExecutionResultType == SandboxResultType.SyntaxError))
                {
                    for (int i = 0; i < 2 && state.CodeExecutionFailuresDetectorIterationCount < 2; i++)
                    {
                        var analysis = await _codeExecutionFailuresDetectorWorkflowStep.ExecuteCodeExecutionFailuresDetectorAsync(state, i + 1);

                        if (analysis.Equals(JavascriptCodeExecutionFailuresDetectorAgent.NO_ERROR, StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        await _codeFixerForRuntimeErrorsWorkflowStep.ExecuteCodeFixerForRuntimeErrorsAsync(state, analysis, i + 1);

                        var sandBoxError = await _jsSandboxWorkflowStep.ExecuteJSSandboxAsync(state, true);
                        if (sandBoxError)
                        {
                            break;
                        }
                    }

                    if (_workflowConfiguration.EnableDomainExpert)
                    {
                        await _domainExpertWorkflowStep.ExecuteDomainExpertAgentAsync(state);
                    }
                    await CompleteWorkflowAsync(state);
                }
                else
                {
                    if (_workflowConfiguration.EnableDomainExpert)
                    {
                        await _domainExpertWorkflowStep.ExecuteDomainExpertAgentAsync(state);
                    }
                    await CompleteWorkflowAsync(state);
                }
                goto WorkflowEnd;
            }
            else if (state.ClassifiedUserRequest.CanonicalizedIntentCategory == UserIntentCategoryValues.Other)
            {
                goto CompleteWorkflow;
            }
            else
            {
                throw new Exception($"Unknown user intent category: {state.ClassifiedUserRequest.CanonicalizedIntentCategory}");
            } // end of if task execution

        CompleteWorkflow:
            await CompleteWorkflowAsync(state);

        WorkflowEnd:

            await _workflowProgressNotifier.NotifyWorkflowEnd();

            return new WorkflowResult
            {
                Response = state.FinalAnswer!,
                UsageStatistics = state.TokenUsageEntries
            };
        }

        private async Task CompleteWorkflowAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Personal Assistant Agent...");
            var originalUserRequest = state.OriginalUserRequest;
            var canonicalizedIntent = state.CanonicalizedIntent;

            string? data = null;
            if (state.ClassifiedUserRequest.CanonicalizedIntentCategory == UserIntentCategoryValues.Documentation)
            {
                data = state.DocumentationContent;
            }
            else if (state.ClassifiedUserRequest.CanonicalizedIntentCategory == UserIntentCategoryValues.TaskExecution)
            {
                if (state.FunctionalAnalystRejected)
                {
                    data = $"""
                            The request made by the user was rejected. The reason for rejection is as follows:
                            {state.FunctionalAnalystRejectReasons}
                            """;
                }
                else if (state.TechnicalAnalystRejected)
                {
                    data = $"""
                            The request made by the user was rejected. The reason for rejection is as follows:
                            {state.TechnicalAnalystRejectReasons}
                            """;
                }
                else if (state.CodeExecutionResultType == SandboxResultType.CallError)
                {
                    data = state.SandboxResult;
                }
                else
                {
                    data = $"""
                            This is the raw output from the code execution sandbox:
                            {state.SandboxResult}
                            """;
                    if (!string.IsNullOrEmpty(state.DomainExpertOutput))
                    {
                        data += $"""
                            ------------------------------------------------------
                            This is the comment from the Domain Expert Agent regarding the code execution result:
                            {state.DomainExpertOutput}
                            """;
                    }
                }
            }

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Personal Assistant Agent", new Dictionary<string, string>
            {
                { "Data", data ?? "(No data)" },
                { "ExecutionError", state.ExecutionError.ToString() },
                { "OriginalUserRequest", originalUserRequest },
                { "CanonicalizedIntent", canonicalizedIntent },
                { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
                { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
                { "LanguageOfTheUser", state.ClassifiedUserRequest.LanguageOfTheUser ?? "(No language specified)" },
                { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" }
            });

            var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
            {
                Data = data,
                ExecutionError = state.ExecutionError,
                LanguageOfTheUser = state.ClassifiedUserRequest.LanguageOfTheUser,
                OriginalUserRequest = originalUserRequest,
                CanonicalizedIntent = canonicalizedIntent,
                SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
                UserPreferences = state.ClassifiedUserRequest.UserPreferences,
                Memories = state.PastMemoriesQueryResults.Select(m => m.Memory)
            });
            state.FinalAnswer = personalAssistantOutput.Response;
            state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount, stopwatch.Elapsed, "Personal Assistant Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "Response", state.FinalAnswer },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch.Elapsed) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Personal Assistant Agent", notifyDictionary);
        }

        private static string GetElapsedTime(TimeSpan elapsed) => $"{elapsed.TotalMilliseconds:0}ms";

        private static string ToBulletList<T>(IEnumerable<T> items)
            => string.Join("\n", items.Select(item => $"- {item}"));

        public string GetIngressExecutorName() => IntentExtractorAgentConfiguration.AgentName;

        public string GetEgressExecutorName() => PersonalAssistantAgentConfiguration.AgentName;
    }
}



