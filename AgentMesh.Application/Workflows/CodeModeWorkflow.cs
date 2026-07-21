using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Models;
using AgentMesh.Models.PersonalAssistant;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Application.Workflows.Steps;
using AgentMesh.Services;

namespace AgentMesh.Application.Workflows
{
    public partial class CodeModeWorkflow(
        ILogger<CodeModeWorkflow> logger,
        IWorkflowProgressNotifier workflowProgressNotifier,
        PersonalAssistantAgent personalAssistantAgent,
        CodeModeWorkflowConfiguration workflowConfiguration,
        AgentMemoryServiceWorkflowStep agentMemoryServiceWorkflowStep,
        DomainsKnowledgeBaseServiceSearchWorkflowStep domainsKnowledgeBaseServiceSearchWorkflowStep,
        DomainsKnowledgeBaseDocumentsExtractorWorkflowStep domainsKnowledgeBaseDocumentsExtractorWorkflowStep,
        DocumentationWorkflowStep documentationWorkflowStep,
        FunctionalAnalystWorkflowStep functionalAnalystWorkflowStep,
        APIsKnowledgeBaseServiceSearchWorkflowStep apisKnowledgeBaseServiceSearchWorkflowStep,
        APIKnowledgeBaseDocumentsExtractorWorkflowStep apiKnowledgeBaseDocumentsExtractorWorkflowStep,
        TechnicalAnalystWorkflowStep technicalAnalystWorkflowStep,
        CoderWorkflowStep coderWorkflowStep,
        JSSandboxWorkflowStep jsSandboxWorkflowStep,
        CodeExecutionFailuresDetectorWorkflowStep codeExecutionFailuresDetectorWorkflowStep,
        CodeFixerForRuntimeErrorsWorkflowStep codeFixerForRuntimeErrorsWorkflowStep,
        DomainExpertWorkflowStep domainExpertWorkflowStep,
        RequestAnalyzerWorkflowStep requestAnalyzerWorkflowStep,
        QueryExpanderWorkflowStep queryExpanderWorkflowStep,
        RequestCanonicalizationWorkflowStep requestCanonicalizationWorkflowStep,
        RerankerWorkflowStep rerankerWorkflowStep) : IWorkflow
    {
        private readonly ILogger<CodeModeWorkflow> _logger = logger;
        private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
        private readonly PersonalAssistantAgent _personalAssistantAgent = personalAssistantAgent;
        private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;

        private readonly AgentMemoryServiceWorkflowStep _agentMemoryServiceWorkflowStep = agentMemoryServiceWorkflowStep;
        private readonly DomainsKnowledgeBaseServiceSearchWorkflowStep _domainsKnowledgeBaseServiceSearchWorkflowStep = domainsKnowledgeBaseServiceSearchWorkflowStep;
        private readonly DomainsKnowledgeBaseDocumentsExtractorWorkflowStep _domainsKnowledgeBaseDocumentsExtractorWorkflowStep = domainsKnowledgeBaseDocumentsExtractorWorkflowStep;
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
        private readonly RequestAnalyzerWorkflowStep _requestAnalyzerWorkflowStep = requestAnalyzerWorkflowStep;
        private readonly QueryExpanderWorkflowStep _queryExpanderWorkflowStep = queryExpanderWorkflowStep;
        private readonly RequestCanonicalizationWorkflowStep _requestCanonicalizationWorkflowStep = requestCanonicalizationWorkflowStep;
        private readonly RerankerWorkflowStep _rerankerWorkflowStep = rerankerWorkflowStep;

        public async Task<WorkflowResult> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory)
        {
            await _workflowProgressNotifier.NotifyWorkflowStart();

            var state = new CodeModeWorkflowState(userInput, chatHistory);

            await _requestAnalyzerWorkflowStep.ExecuteRequestAnalyzerAsync(state, chatHistory);

            if (state.UserRequest!.IntentCategory == AgentMesh.Models.RequestAnalysis.UserIntentCategory.Other)
            {
                goto CompleteWorkflow;
            }

            await _queryExpanderWorkflowStep.ExecuteQueryExpanderAsync(state);

            var memoryTask = (_workflowConfiguration.EnableMemoryService && state.PastMemoriesQuery.Any())
                ? _agentMemoryServiceWorkflowStep.ExecuteAgentMemoryServiceAsync(state)
                : Task.CompletedTask;

            var knowledgeBaseTask = state.DomainsKnowledgeBaseQuery.Any()
                ? _domainsKnowledgeBaseServiceSearchWorkflowStep.ExecuteDomainsKnowledgeBaseServiceSearchAsync(state)
                : Task.CompletedTask;

            await Task.WhenAll(memoryTask, knowledgeBaseTask);

            if (state.DomainsKnowledgeBaseQueryResults.Results.Any())
            {
                await _rerankerWorkflowStep.ExecuteRerankerAsync(state);
            }

            if (state.DomainsKnowledgeBaseQueryResults.Results.Any())
            {
                await _domainsKnowledgeBaseDocumentsExtractorWorkflowStep.ExecuteDomainsKnowledgeBaseDocumentsExtractorAsync(state);
            }

            if (state.IntentCategory == AgentMesh.Models.RequestAnalysis.UserIntentCategory.Documentation)
            {
                await _documentationWorkflowStep.ExecuteDocumentationAgentAsync(state);
            }
            else if (state.IntentCategory == AgentMesh.Models.RequestAnalysis.UserIntentCategory.TaskExecution)
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
                else if (state.CodeExecutionResultType == SandboxResultType.ApplicationError ||
                         state.CodeExecutionResultType == SandboxResultType.SyntaxError)
                {
                    if (_workflowConfiguration.EnableCodeCorrection)
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
            else if (state.IntentCategory == AgentMesh.Models.RequestAnalysis.UserIntentCategory.Other)
            {
                goto CompleteWorkflow;
            }
            else
            {
                throw new Exception($"Unknown user intent category: {state.IntentCategory}");
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
            var canonicalizedIntent = state.Intent;

            string? data = null;
            var requestFailed = false;
            string? requestFailureReason = null;

            if (state.IntentCategory == AgentMesh.Models.RequestAnalysis.UserIntentCategory.Documentation)
            {
                data = state.DocumentationContent;
            }
            else if (state.IntentCategory == AgentMesh.Models.RequestAnalysis.UserIntentCategory.TaskExecution)
            {
                if (state.FunctionalAnalystRejected)
                {
                    requestFailed = true;
                    requestFailureReason = $"""
                            The request made by the user was rejected. The reason for rejection is as follows:
                            {state.FunctionalAnalystRejectReasons}
                            """;
                }
                else if (state.TechnicalAnalystRejected)
                {
                    requestFailed = true;
                    requestFailureReason = $"""
                            The request made by the user was rejected. The reason for rejection is as follows:
                            {state.TechnicalAnalystRejectReasons}
                            """;
                }
                else if (state.CodeExecutionResultType != SandboxResultType.Success)
                {
                    requestFailed = true;
                    requestFailureReason = state.SandboxResult;
                }
                else
                {
                    data = $"""
                            {state.SandboxResult}
                            """;
                    if (!string.IsNullOrEmpty(state.DomainExpertOutput))
                    {
                        data += $"""

                            {state.DomainExpertOutput}
                            """;
                    }
                }
            }

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Personal Assistant Agent", new Dictionary<string, string>
            {
                { "Data", data ?? "(No data)" },
                { "RequestFailed", requestFailed.ToString() },
                { "RequestFailureReason", requestFailureReason ?? "(No failure reason)" },
                { "CanonicalizedIntent", canonicalizedIntent },
                { "ConversationTopic", state.ConversationTopic },
                { "UserPreferences", state.UserPreferences.Any() ? ToBulletList(state.UserPreferences) : "(No user preferences)" },
                { "UserProvidedData", state.UserProvidedData.Any() ? ToBulletList(state.UserProvidedData) : "(No user provided data)" },
                { "UserRequestedActions", state.UserRequestedActions.Any() ? ToBulletList(state.UserRequestedActions) : "(No user requested actions)" },
                { "LanguageOfTheUser", state.LanguageOfTheUser },
                { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" }
            });

            var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
            {
                Data = data,
                RequestFailed = requestFailed,
                RequestFailureReason = requestFailureReason,
                LanguageOfTheUser = state.LanguageOfTheUser,
                CanonicalizedIntent = canonicalizedIntent,
                ConversationTopic = state.ConversationTopic,
                UserPreferences = state.UserPreferences,
                UserProvidedData = state.UserProvidedData,
                UserRequestedActions = state.UserRequestedActions,
                Memories = state.PastMemoriesQueryResults.Select(m => m.Memory)
            });

            state.PersonalAssistantOpeningSentence = personalAssistantOutput.OpeningSentence;
            state.PersonalAssistantClosingSentence = personalAssistantOutput.ClosingSentence;
            state.PersonalAssistantConvenienceErrorSentence = personalAssistantOutput.ConvenienceErrorSentence;

            if (requestFailed)
            {
                state.FinalAnswer = personalAssistantOutput.ConvenienceErrorSentence;
            }
            else
            {
                state.FinalAnswer = string.Join(Environment.NewLine + Environment.NewLine,
                    new[] { personalAssistantOutput.OpeningSentence, data, personalAssistantOutput.ClosingSentence }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount, stopwatch.Elapsed, "Personal Assistant Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "OpeningSentence", state.PersonalAssistantOpeningSentence ?? string.Empty },
                { "ClosingSentence", state.PersonalAssistantClosingSentence ?? string.Empty },
                { "ConvenienceErrorSentence", state.PersonalAssistantConvenienceErrorSentence ?? string.Empty },
                { "Response", state.FinalAnswer ?? string.Empty },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch.Elapsed) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Personal Assistant Agent", notifyDictionary);
        }

        private static string GetElapsedTime(TimeSpan elapsed) => $"{elapsed.TotalMilliseconds:0}ms";

        private static string ToBulletList<T>(IEnumerable<T> items)
            => string.Join("\n", items.Select(item => $"- {item}"));

        public string GetIngressExecutorName() => RequestAnalyzerAgentConfiguration.AgentName;

        public string GetEgressExecutorName() => PersonalAssistantAgentConfiguration.AgentName;
    }
}
