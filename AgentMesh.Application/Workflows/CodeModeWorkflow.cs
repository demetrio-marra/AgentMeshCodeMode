using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Configuration;
using AgentMesh.Models;
using AgentMesh.Models.CodeExecutionFailuresDetector;
using AgentMesh.Models.CodeFixer;
using AgentMesh.Models.Coder;
using AgentMesh.Models.CodeSandbox;
using AgentMesh.Models.Documentation;
using AgentMesh.Models.IntentCanonicalization;
using AgentMesh.Models.IntentExtractor;
using AgentMesh.Models.PersonalAssistant;
using AgentMesh.Models.ResultsPresenter;
using AgentMesh.Models.RequirementsCollector;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using AgentMesh.Models.KnowledgeBase;
using System.Diagnostics;
using AgentMesh.Models.AgentMemory;
using System.Data;
using AgentMesh.Models.QueriesCache;
using AgentMesh.Models.FunctionalAnalyst;
using AgentMesh.Models.TechnicalAnalyst;

namespace AgentMesh.Application.Workflows
{
    public partial class CodeModeWorkflow(ILogger<CodeModeWorkflow> logger,
        IWorkflowProgressNotifier workflowProgressNotifier,
        IFunctionalAnalystAgent functionalAnalystAgent,
        ITechnicalAnalystAgent technicalAnalystAgent,
        IDocumentationAgent documentationAgent,
        ICoderAgent coderAgent,
        ICodeFixerAgent codeFixerAgent,
        ICodeExecutionFailuresDetectorAgent codeExecutionFailuresDetectorAgent,
        IResultsPresenterAgent resultsPresenterAgent,
        IJSSandboxExecutor jsSandboxExecutor,
        IIntentExtractorAgent intentExtractorAgent,
        IIntentCanonicalizationAgent intentCanonicalizationAgent,
        IRequirementsCollectorAgent requirementsCollectorAgent,
        IPersonalAssistantAgent personalAssistantAgent,
        IAgentMemoryRetrieverExecutor agentMemoryRetriever,
        IKnowledgeBaseSearchExecutor knowledgeBaseSearchExecutor,
        IKnowledgeBaseGetDocsExecutor knowledgeBaseGetDocsExecutor,
        IKnowledgeBaseSearchFastExecutor knowledgeBaseSearchFastExecutor,
        CodeModeWorkflowConfiguration workflowConfiguration,
        IQueriesCacheService queriesCacheService) : IWorkflow
    {
        private const string DOMAINS_DOCUMENTATION_COLLECTION_NAME = "domains";
        private const string APIS_DOCUMENTATION_COLLECTION_NAME = "apis";

        private readonly ILogger<CodeModeWorkflow> _logger = logger;
        private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;

        private readonly IFunctionalAnalystAgent _functionalAnalystAgent = functionalAnalystAgent;
        private readonly ITechnicalAnalystAgent _technicalAnalystAgent = technicalAnalystAgent;
        private readonly IDocumentationAgent _documentationAgent = documentationAgent;
        private readonly ICoderAgent _coderAgent = coderAgent;
        private readonly ICodeFixerAgent _codeFixerAgent = codeFixerAgent;
        private readonly ICodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent = codeExecutionFailuresDetectorAgent;
        private readonly IResultsPresenterAgent _resultsPresenterAgent = resultsPresenterAgent;
        private readonly IJSSandboxExecutor _jsSandboxExecutor = jsSandboxExecutor;
        private readonly IIntentExtractorAgent _intentExtractorAgent = intentExtractorAgent;
        private readonly IIntentCanonicalizationAgent _intentCanonicalizationAgent = intentCanonicalizationAgent;
        private readonly IRequirementsCollectorAgent _requirementsCollectorAgent = requirementsCollectorAgent;
        private readonly IPersonalAssistantAgent _personalAssistantAgent = personalAssistantAgent;
        private readonly IAgentMemoryRetrieverExecutor _agentMemoryRetriever = agentMemoryRetriever;
        private readonly IKnowledgeBaseSearchExecutor _knowledgeBaseSearchExecutor = knowledgeBaseSearchExecutor;
        private readonly IKnowledgeBaseGetDocsExecutor _knowledgeBaseGetDocsExecutor = knowledgeBaseGetDocsExecutor;
        private readonly IKnowledgeBaseSearchFastExecutor _knowledgeBaseSearchFastExecutor = knowledgeBaseSearchFastExecutor;
        private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;
        private readonly IQueriesCacheService _queriesCacheService = queriesCacheService;

        public async Task<WorkflowResult> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory)
        {
            await _workflowProgressNotifier.NotifyWorkflowStart();

            var state = new CodeModeWorkflowState(userInput, chatHistory);

            await ExecuteIntentExtractorAsync(state, chatHistory);

            if (state.ClassifiedUserRequest.IntentCategory == UserIntentCategoryValues.Other)
            {
                goto CompleteWorkflow;
            }

            if (state.ClassifiedUserRequest.EntitiesByDomain.Any())
            {
                await ExecuteDomainsKnowledgeBaseServiceFastSearchAsync(state);
            }

            await ExecuteRequirementsCollectorAsync(state);

            var memoryTask = (_workflowConfiguration.EnableMemoryService && state.PastMemoriesQuery.Any())
                ? ExecuteAgentMemoryServiceAsync(state)
                : Task.CompletedTask;

            var knowledgeBaseTask = state.DomainsKnowledgeBaseQuery.Any()
                ? ExecuteDomainsKnowledgeBaseServiceSearchAsync(state)
                : Task.CompletedTask;

            await Task.WhenAll(memoryTask, knowledgeBaseTask);

            await ExecuteIntentCanonicalizationAsync(state);

            if (state.DomainsKnowledgeBaseQueryResults.Results.Any())
            {
                await ExecuteDomainsKnowledgeBaseDocumentsExtractorAsync(state);
            }

            if (state.ClassifiedUserRequest.CanonicalizedIntentCategory == UserIntentCategoryValues.Documentation)
            {
                await ExecuteDocumentationAgentAsync(state);
            }
            else if (state.ClassifiedUserRequest.CanonicalizedIntentCategory == UserIntentCategoryValues.TaskExecution)
            {
                var functionalAnalystTask = ExecuteFunctionalAnalystAsync(state);
                var technicalAnalystTask = ExecuteTechnicalAnalystAsync(state);

                await Task.WhenAll(functionalAnalystTask, technicalAnalystTask);

                if (state.APISKnowledgeBaseQuery.Any())
                {
                    await ExecuteAPIsKnowledgeBaseServiceSearchAsync(state);
                    await ExecuteAPIKnowledgeBaseDocumentsExtractorAsync(state);
                }

                await ExecuteCoderAsync(state);

                await ExecuteJSSandboxAsync(state, false);

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
                        var analysis = await ExecuteCodeExecutionFailuresDetectorAsync(state, i + 1);

                        if (analysis.Equals(JavascriptCodeExecutionFailuresDetectorAgent.NO_ERROR, StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        await ExecuteCodeFixerForRuntimeErrorsAsync(state, analysis, i + 1);

                        var sandBoxError = await ExecuteJSSandboxAsync(state, true);
                        if (sandBoxError)
                        {
                            break;
                        }
                    }

                    await ExecuteResultsPresenterAsync(state);
                    await CompleteWorkflowAsync(state);
                }
                else
                {
                    await ExecuteResultsPresenterAsync(state);
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

        private async Task ExecuteDomainsKnowledgeBaseDocumentsExtractorAsync(CodeModeWorkflowState state)
        {
            await ExecuteKnowledgeBaseDocumentsExtractorAsync(
                state,
                _logger,
                _workflowProgressNotifier,
                _knowledgeBaseGetDocsExecutor,
                "Engaging Knowledge Base Documents Extractor Service...",
                "KB Documents Extractor Service (Domain)",
                "Documents",
                workflowState => workflowState.DomainsKnowledgeBaseQueryResults.Results.Select(r => r.File),
                file => file?.Trim() ?? string.Empty,
                StringComparer.Ordinal,
                results => results
                    .Where(doc => !string.IsNullOrWhiteSpace(doc.File))
                    .GroupBy(doc => doc.File!)
                    .ToDictionary(
                        group => group.Key,
                        group => new KnowledgeBaseDocumentContent
                        {
                            File = group.Key,
                            Content = group.First().Content
                        }),
                (workflowState, documents) => workflowState.DomainsKnowledgeBaseDocumentsContent = documents);
        }

        private async Task ExecuteAPIKnowledgeBaseDocumentsExtractorAsync(CodeModeWorkflowState state)
        {
            await ExecuteKnowledgeBaseDocumentsExtractorAsync(
                state,
                _logger,
                _workflowProgressNotifier,
                _knowledgeBaseGetDocsExecutor,
                "Engaging Knowledge Base API Documents Extractor Service...",
                "KB Documents Extractor Service (APIs)",
                "Documents",
                workflowState => workflowState.APISKnowledgeBaseQueryResults.Results.Select(r => r.File),
                file => file?.Trim() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase,
                results => results
                    .Where(doc => !string.IsNullOrWhiteSpace(doc.File))
                    .GroupBy(doc => doc.File!, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => new KnowledgeBaseDocumentContent
                        {
                            File = group.Key,
                            Content = group.First().Content
                        },
                        StringComparer.OrdinalIgnoreCase),
                (workflowState, documents) => workflowState.KnowledgeBaseAPIDocumentsContent = documents);
        }

        private static async Task ExecuteKnowledgeBaseDocumentsExtractorAsync(
            CodeModeWorkflowState state,
            ILogger logger,
            IWorkflowProgressNotifier workflowProgressNotifier,
            IKnowledgeBaseGetDocsExecutor knowledgeBaseGetDocsExecutor,
            string logMessage,
            string stepName,
            string startNotificationKey,
            Func<CodeModeWorkflowState, IEnumerable<string>> getFilePaths,
            Func<string?, string> normalizeFilePath,
            StringComparer distinctComparer,
            Func<IEnumerable<KnowledgeBaseGetDocsOutputItem>, Dictionary<string, KnowledgeBaseDocumentContent>> buildDocumentsByFile,
            Action<CodeModeWorkflowState, IReadOnlyCollection<KnowledgeBaseDocumentContent>> setDocuments)
        {
            var stopwatch = Stopwatch.StartNew();
            logger.LogDebug(logMessage);

            var filesToExtract = getFilePaths(state)
                .Select(normalizeFilePath)
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .Distinct(distinctComparer)
                .ToList();

            await workflowProgressNotifier.NotifyWorkflowStepStart(stepName, new Dictionary<string, string>
            {
                { startNotificationKey, ToBulletList(filesToExtract) }
            });

            var fetchedFilesContent = await knowledgeBaseGetDocsExecutor.ExecuteAsync(new KnowledgeBaseGetDocsInput
            {
                FilePaths = filesToExtract
            });

            var documentsByFile = buildDocumentsByFile(fetchedFilesContent.Results);
            var documents = documentsByFile.Values.ToList();
            setDocuments(state, documents);

            state.AddStepUsage(stepName, stopwatch.Elapsed, false);

            var notifyDictionary = new Dictionary<string, string>
            {
                { "Total files extracted", ToBulletList(documents.Select(doc => doc.File)) },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
        }

        private async Task ExecuteIntentExtractorAsync(CodeModeWorkflowState state, IEnumerable<ContextMessage> chatHistory)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Intent Extractor Agent...");

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Intent Extractor Agent", new Dictionary<string, string>
            {
                { "ContextMessages", "<omitted for brevity>. Total: " + chatHistory.Count().ToString() },
                { "UserLastRequest", state.UserLastRequest },
                { "LanguageOfKnowledgeBase", _workflowConfiguration.LanguageOfKnowledgeBase }
            });

            var intentExtractorOutput = await _intentExtractorAgent.ExecuteAsync(new IntentExtractorAgentInput
            {
                ContextMessages = [.. state.InitialContextMessages],
                UserLastRequest = state.UserLastRequest,
                ApplicationDomainList = _workflowConfiguration.ApplicationDomainList,
                LanguageOfKnowledgeBase = _workflowConfiguration.LanguageOfKnowledgeBase
            });

            state.ClassifiedUserRequest = new StructuredUserRequest
            {
                OriginalUserRequest = intentExtractorOutput.OriginalUserRequest,
                Intent = intentExtractorOutput.UserIntent,
                IntentCategory = intentExtractorOutput.UserIntentCategory,
                CanonicalizedIntentCategory = intentExtractorOutput.UserIntentCategory,
                LanguageOfTheUser = intentExtractorOutput.LanguageOfTheUser,
                EntitiesByDomain = intentExtractorOutput.EntitiesByDomain,
                SupportingIntentInformation = intentExtractorOutput.SupportingIntentInformation,
                UserPreferences = intentExtractorOutput.UserPreferences,
                MissingMemories = intentExtractorOutput.MissingMemories
            };
            state.CanonicalizedIntent = state.ClassifiedUserRequest.Intent ?? string.Empty;

            state.AddTokenUsage(IntentExtractorAgentConfiguration.AgentName, intentExtractorOutput.InputTokenCount, intentExtractorOutput.OutputTokenCount, stopwatch.Elapsed, "Intent Extractor Agent");

            var notifyDictionary = new Dictionary<string, string>
            {
                { "OriginalUserRequest", state.ClassifiedUserRequest.OriginalUserRequest },
                { "ExtractedIntent", state.ClassifiedUserRequest.Intent ?? "(No intent extracted)" }
            };
            if (state.ClassifiedUserRequest.LanguageOfTheUser != null)
            {
                notifyDictionary.Add("LanguageOfTheUser", state.ClassifiedUserRequest.LanguageOfTheUser);
            }
            notifyDictionary.Add("UserIntentCategory", state.ClassifiedUserRequest.IntentCategory.ToString());
            if (state.ClassifiedUserRequest.SupportingIntentInformation.Any())
            {
                notifyDictionary.Add("SupportingIntentInformation", ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation));
            }
            if (state.ClassifiedUserRequest.EntitiesByDomain.Any())
            {
                notifyDictionary.Add("EntitiesByDomain", ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp =>
                    kvp.Value.Select(entity => $"[{kvp.Key}] {entity}"))));
            }
            if (state.ClassifiedUserRequest.UserPreferences.Any())
            {
                notifyDictionary.Add("UserPreferences", ToBulletList(state.ClassifiedUserRequest.UserPreferences));
            }
            if (state.ClassifiedUserRequest.MissingMemories.Any())
            {
                notifyDictionary.Add("MissingMemories", ToBulletList(state.ClassifiedUserRequest.MissingMemories));
            }
            notifyDictionary.Add("ELAPSED_TIME", GetElapsedTime(stopwatch));
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Intent Extractor Agent", notifyDictionary);
        }

        private async Task ExecuteIntentCanonicalizationAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Intent Canonicalization Agent...");

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Intent Canonicalization Agent", new Dictionary<string, string>
            {
                { "Intent", state.ClassifiedUserRequest.Intent ?? "(No intent)" },
                { "EntitiesByDomain", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(e => $"[{kvp.Key}] {e}"))) : "(No entities)" },
                { "KnowledgeBaseResults", state.DomainsKnowledgeBaseQueryResults.Results.Any() ? ToBulletList(state.DomainsKnowledgeBaseQueryResults.Results.Select(r => $"[{r.File}] {r.Title}")) : "(No knowledge base results)" }
            });

            var output = await _intentCanonicalizationAgent.ExecuteAsync(new IntentCanonicalizationAgentInput
            {
                Intent = state.ClassifiedUserRequest.Intent ?? string.Empty,
                UserIntentCategory = state.ClassifiedUserRequest.IntentCategory,
                EntitiesByDomain = state.ClassifiedUserRequest.EntitiesByDomain,
                SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
                FastDomainsKnowledgeBaseQueryResults = state.FastDomainsKnowledgeBaseQueryResults.Results
            });

            state.CanonicalizedIntent = output.DomainedIntent;
            state.CanonicalizedIntentCategory = output.CanonicalizedIntentCategory;
            state.AddTokenUsage(IntentCanonicalizationAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Intent Canonicalization Agent");

            var notifyDictionary = new Dictionary<string, string>
            {
                { "DomainedIntent", state.CanonicalizedIntent },
                { "CanonicalizedIntentCategory", state.CanonicalizedIntentCategory.ToString() },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Intent Canonicalization Agent", notifyDictionary);
        }

        private async Task ExecuteRequirementsCollectorAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Requirements Collector Agent...");

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Requirements Collector Agent", new Dictionary<string, string>
            {
                { "UserIntent", state.ClassifiedUserRequest.Intent },
                { "UserIntentCategory", state.ClassifiedUserRequest.IntentCategory.ToString() },
                { "EntitiesByDomain", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(e => $"[{kvp.Key}] {e}"))) : "(No entities)" },
                { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
                { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
                { "MissingMemories", state.ClassifiedUserRequest.MissingMemories.Any() ? ToBulletList(state.ClassifiedUserRequest.MissingMemories) : "(No missing memories)" },
                { "FastKnowledgeBaseResults", state.FastDomainsKnowledgeBaseQueryResults.Results.Any() ? ToBulletList(state.FastDomainsKnowledgeBaseQueryResults.Results.Select(r => $"[{r.File}] {r.Title}")) : "(No fast knowledge base results)" }
            });

            var output = await _requirementsCollectorAgent.ExecuteAsync(new RequirementsCollectorAgentInput
            {
                UserIntent = state.CanonicalizedIntent,
                UserIntentCategory = state.ClassifiedUserRequest.IntentCategory,
                EntitiesByDomain = state.ClassifiedUserRequest.EntitiesByDomain,
                SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
                UserPreferences = state.ClassifiedUserRequest.UserPreferences,
                MissingMemories = state.ClassifiedUserRequest.MissingMemories,
                FastKnowledgeBaseQueryResults = state.FastDomainsKnowledgeBaseQueryResults.Results,
                LanguageOfKnowledgeBase = _workflowConfiguration.LanguageOfKnowledgeBase
            });

            state.PastMemoriesQuery = output.MissingPastMemories;
            state.DomainsKnowledgeBaseQuery = output.MissingKnowledgeBaseSearchEntries;

            state.AddTokenUsage(RequirementsCollectorAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Requirements Collector Agent");

            var notifyDictionary = new Dictionary<string, string>();
            if (state.PastMemoriesQuery.Any())
            {
                notifyDictionary.Add("MissingPastMemoriesDetails", ToBulletList(state.PastMemoriesQuery));
            }
            if (state.DomainsKnowledgeBaseQuery.Any())
            {
                notifyDictionary.Add("MissingKnowledgeBaseEntriesDetails", ToBulletList(state.DomainsKnowledgeBaseQuery));
            }
            notifyDictionary.Add("ELAPSED_TIME", GetElapsedTime(stopwatch));
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Requirements Collector Agent", notifyDictionary);
        }

        private async Task ExecuteAgentMemoryServiceAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Agent Memory Service...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Agent Memory Service", new Dictionary<string, string>
            {
                { "MissingPastMemories", ToBulletList(state.PastMemoriesQuery) }
            });

            var queriesList = state.PastMemoriesQuery.ToList();

            var brcOutput = await _agentMemoryRetriever.ExecuteAsync(new AgentMemoryRetrieverInput
            {
                Query = string.Join(", ", queriesList)
            });

            var retrievedMemories = brcOutput.Items.ToList();
            state.PastMemoriesQueryResults = state.PastMemoriesQueryResults.Concat(retrievedMemories).ToList();

            if (_workflowConfiguration.EnableCacheService && retrievedMemories.Any())
            {
                var cacheItems = queriesList
                    .Zip(retrievedMemories, (query, result) => new AgentMemoryQueriesCacheItem
                    {
                        FoundQuery = query,
                        Result = result.Memory
                    })
                    .ToList();

                var cacheUpdateResult = await _queriesCacheService.SetMemoryCachedItemsAsync(cacheItems);
                
                var tokenUsageInfo = new AgentTokenUsageEntry
                {
                    AgentName = "Query Cache Updater Service (Memory)",
                    InputTokens = cacheUpdateResult.TotalTokens,
                    OutputTokens = 0
                };
                state.AddStepUsage("Agent Memory Service", stopwatch.Elapsed, true, tokenUsageInfo);
            }
            else
            {
                state.AddStepUsage("Agent Memory Service", stopwatch.Elapsed, false);
            }

            var notifyDictionary = new Dictionary<string, string>
            {
                { "ExtractedAgentMemories", ToBulletList(retrievedMemories.Select(m => m.Memory)) },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Agent Memory Service", notifyDictionary);
        }


        #region hybrid search executors

        private async Task ExecuteDomainsKnowledgeBaseServiceSearchAsync(CodeModeWorkflowState state)
        {
            await ExecuteKnowledgeBaseServiceSearchAsync(
                state,
                _logger,
                _workflowProgressNotifier,
                _knowledgeBaseSearchExecutor,
                _queriesCacheService,
                _workflowConfiguration.EnableCacheService,
                "KB Search Service",
                DOMAINS_DOCUMENTATION_COLLECTION_NAME,
                workflowState => workflowState.DomainsKnowledgeBaseQuery,
                workflowState => workflowState.DomainsKnowledgeBaseQueryResults,
                (workflowState, queryResult) => workflowState.DomainsKnowledgeBaseQueryResults = queryResult);
        }


        private async Task ExecuteAPIsKnowledgeBaseServiceSearchAsync(CodeModeWorkflowState state)
        {
            await ExecuteKnowledgeBaseServiceSearchAsync(
                state,
                _logger,
                _workflowProgressNotifier,
                _knowledgeBaseSearchExecutor,
                _queriesCacheService,
                _workflowConfiguration.EnableCacheService,
                "APIs Knowledge Base Service",
                APIS_DOCUMENTATION_COLLECTION_NAME,
                workflowState => workflowState.APISKnowledgeBaseQuery,
                workflowState => workflowState.APISKnowledgeBaseQueryResults,
                (workflowState, queryResult) => workflowState.APISKnowledgeBaseQueryResults = queryResult);
        }



        private static async Task ExecuteKnowledgeBaseServiceSearchAsync(
            CodeModeWorkflowState state,
            ILogger logger,
            IWorkflowProgressNotifier workflowProgressNotifier,
            IKnowledgeBaseSearchExecutor knowledgeBaseSearchExecutor,
            IQueriesCacheService queriesCacheService,
            bool enableCacheService,
            string stepName,
            string collectionName,
            Func<CodeModeWorkflowState, IEnumerable<KnowledgeBaseQueryInputItem>> getQueries,
            Func<CodeModeWorkflowState, KnowledgeBaseQueryResult> getExistingResults,
            Action<CodeModeWorkflowState, KnowledgeBaseQueryResult> setResults)
        {
            var stopwatch = Stopwatch.StartNew();
            logger.LogDebug("Engaging Knowledge Base Service...");
            await workflowProgressNotifier.NotifyWorkflowStepStart(stepName, new Dictionary<string, string>
            {
                { "MissingKnowledgeBaseEntries", ToBulletList(getQueries(state)) }
            });

            var queriesList = getQueries(state).ToList();

            KnowledgeBaseQueryInput queryInput = new()
            {
                Collections = [collectionName],
                UserIntent = state.CanonicalizedIntent,
                Queries = queriesList
            };

            var brcOutput = await knowledgeBaseSearchExecutor.ExecuteAsync(queryInput, CancellationToken.None);

            var existingResults = getExistingResults(state).Results.ToList();
            setResults(state, new KnowledgeBaseQueryResult
            {
                Results = existingResults.Concat(brcOutput.Results).ToList()
            });

            var cacheTokenUsageInfo = await BuildKnowledgeBaseCacheTokenUsageAsync(enableCacheService, queriesList, brcOutput.Results, queriesCacheService);
            state.AddStepUsage(stepName, stopwatch.Elapsed, cacheTokenUsageInfo is not null, cacheTokenUsageInfo);

            var notifyDictionary = new Dictionary<string, string>
            {
                { "ExtractedKnowledgeBaseEntries", ToBulletList(brcOutput.Results.Select(m => $"File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")) },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
        }

        #endregion

        private static async Task<AgentTokenUsageEntry?> BuildKnowledgeBaseCacheTokenUsageAsync(
            bool enableCacheService,
            IReadOnlyCollection<KnowledgeBaseQueryInputItem> queries,
            IEnumerable<KnowledgeBaseQueryResultItem> queryResults,
            IQueriesCacheService queriesCacheService)
        {
            var resultsList = queryResults.ToList();
            if (!enableCacheService || !resultsList.Any())
            {
                return null;
            }

            var cacheableQueries = queries
                .Where(entry => entry.SearchType != KnowledgeBaseQuerySearchType.Keyword)
                .ToList();

            if (!cacheableQueries.Any())
            {
                return null;
            }

            var cacheItems = new List<KnowledgeBaseQueriesCacheItem>();
            foreach (var query in cacheableQueries)
            {
                foreach (var result in resultsList)
                {
                    cacheItems.Add(new KnowledgeBaseQueriesCacheItem
                    {
                        FoundQuery = query.Query,
                        FoundQueryType = query.SearchType,
                        DocumentId = result.Id,
                        DocumentFile = result.File,
                        DocumentTitle = result.Title,
                        DocumentSummary = result.Summary
                    });
                }
            }

            var cacheUpdateResult = await queriesCacheService.SetKnowledgeBaseCachedItemsAsync(cacheItems);

            return new AgentTokenUsageEntry
            {
                AgentName = "Query Cache Updater Service (Knowledge)",
                InputTokens = cacheUpdateResult.TotalTokens,
                OutputTokens = 0
            };
        }

        #region fast search executors

        private async Task ExecuteDomainsKnowledgeBaseServiceFastSearchAsync(CodeModeWorkflowState state)
        {
            await ExecuteKnowledgeBaseServiceFastSearchAsync(
                state,
                _logger,
                _workflowProgressNotifier,
                _knowledgeBaseSearchFastExecutor,
                _queriesCacheService,
                _workflowConfiguration.EnableCacheService,
                "Engaging Knowledge Base Fast Service...",
                "No domains or entities to search for in knowledge base",
                "KB Fast Search Service",
                "Domains",
                workflowState => ToBulletList(workflowState.ClassifiedUserRequest.EntitiesByDomain.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}")),
                "ExtractedKnowledgeBaseEntries",
                "FastKnowledgeBaseQueryResults",
                DOMAINS_DOCUMENTATION_COLLECTION_NAME,
                workflowState => workflowState.ClassifiedUserRequest.EntitiesByDomain
                    .SelectMany(domainEntry =>
                        new[] { domainEntry.Key }
                            .Concat(domainEntry.Value)
                            .Select(entry => new KnowledgeBaseQueryInputItem
                            {
                                Query = entry,
                                SearchType = KnowledgeBaseQuerySearchType.Keyword
                            })),
                (workflowState, queryResult) => workflowState.FastDomainsKnowledgeBaseQueryResults = queryResult);
        }

        private async Task ExecuteAPIsKnowledgeBaseServiceFastSearchAsync(CodeModeWorkflowState state)
        {
            await ExecuteKnowledgeBaseServiceFastSearchAsync(
                state,
                _logger,
                _workflowProgressNotifier,
                _knowledgeBaseSearchFastExecutor,
                _queriesCacheService,
                _workflowConfiguration.EnableCacheService,
                "Engaging Knowledge Base Fast Service for APIs...",
                "No APIs to search for in knowledge base",
                "API Fast Search Service",
                "APIs",
                workflowState => ToBulletList(workflowState.FastAPISKnowledgeBaseQuery),
                "FastAPISKnowledgeBaseQueryResults",
                "FastAPISKnowledgeBaseQueryResults",
                APIS_DOCUMENTATION_COLLECTION_NAME,
                workflowState => workflowState.FastAPISKnowledgeBaseQuery
                    .Where(query => !string.IsNullOrWhiteSpace(query))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(query => new KnowledgeBaseQueryInputItem
                    {
                        Query = query,
                        SearchType = KnowledgeBaseQuerySearchType.Keyword
                    }),
                (workflowState, queryResult) => workflowState.FastAPISKnowledgeBaseQueryResults = queryResult);
        }

        private static async Task ExecuteKnowledgeBaseServiceFastSearchAsync(
            CodeModeWorkflowState state,
            ILogger logger,
            IWorkflowProgressNotifier workflowProgressNotifier,
            IKnowledgeBaseSearchFastExecutor knowledgeBaseSearchFastExecutor,
            IQueriesCacheService queriesCacheService,
            bool enableCacheService,
            string logMessage,
            string noQueriesLogMessage,
            string stepName,
            string startNotificationKey,
            Func<CodeModeWorkflowState, string> getStartNotificationValue,
            string emptyResultNotificationKey,
            string resultsNotificationKey,
            string collectionName,
            Func<CodeModeWorkflowState, IEnumerable<KnowledgeBaseQueryInputItem>> buildQueries,
            Action<CodeModeWorkflowState, KnowledgeBaseQueryResult> setResults)
        {
            var stopwatch = Stopwatch.StartNew();
            logger.LogDebug(logMessage);

            await workflowProgressNotifier.NotifyWorkflowStepStart(stepName, new Dictionary<string, string>
            {
                { startNotificationKey, getStartNotificationValue(state) }
            });

            var queries = buildQueries(state).ToList();

            if (!queries.Any())
            {
                logger.LogDebug(noQueriesLogMessage);
                state.AddStepUsage(stepName, stopwatch.Elapsed, false);
                await workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, new Dictionary<string, string>
                {
                    { emptyResultNotificationKey, "(No queries generated)" },
                    { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
                });
                return;
            }

            KnowledgeBaseQueryInput queryInput = new()
            {
                UserIntent = state.CanonicalizedIntent,
                Queries = queries,
                Collections = [collectionName]
            };

            var brcOutput = await knowledgeBaseSearchFastExecutor.ExecuteAsync(queryInput, CancellationToken.None);

            setResults(state, new KnowledgeBaseQueryResult
            {
                Results = brcOutput.Results.ToList()
            });

            var cacheTokenUsageInfo = await BuildKnowledgeBaseCacheTokenUsageAsync(enableCacheService, queries, brcOutput.Results, queriesCacheService);
            state.AddStepUsage(stepName, stopwatch.Elapsed, cacheTokenUsageInfo is not null, cacheTokenUsageInfo);

            var notifyDictionary = new Dictionary<string, string>
            {
                { resultsNotificationKey, ToBulletList(brcOutput.Results.Select(m => $"File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")) },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
        }

        #endregion


        private async Task ExecuteFunctionalAnalystAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Functional Analyst Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Functional Analyst Agent", new Dictionary<string, string>
            {
                { "Intent", state.CanonicalizedIntent },
                { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
                { "Entities", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(v => $"[{kvp.Key}] {v}"))) : "(No entities)" },
                { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
                { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" },
                { "KnowledgeBaseDocumentsContent", state.DomainsKnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var functionalAnalystOutput = await _functionalAnalystAgent.ExecuteAsync(new FunctionalAnalystAgentInput
            {
                Intent = state.CanonicalizedIntent,
                SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
                Entities = state.ClassifiedUserRequest.EntitiesByDomain,
                UserPreferences = state.ClassifiedUserRequest.UserPreferences,
                AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = string.Join("------" + Environment.NewLine, state.DomainsKnowledgeBaseDocumentsContent.Select(doc => doc.Print()))
            }, cancellationToken);

            state.ShouldEngageCoder = true;
            state.BusinessRequirements = functionalAnalystOutput.BusinessRequirements;
            state.AddTokenUsage(FunctionalAnalystAgentConfiguration.AgentName, functionalAnalystOutput.InputTokenCount, functionalAnalystOutput.OutputTokenCount, stopwatch.Elapsed, "Functional Analyst Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "BusinessRequirements", state.BusinessRequirements ?? "(No business requirements)" },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Functional Analyst Agent", notifyDictionary);
        }

        private async Task ExecuteTechnicalAnalystAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Technical Analyst Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Technical Analyst Agent", new Dictionary<string, string>
            {
                { "Intent", state.CanonicalizedIntent },
                { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
                { "Entities", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(v => $"[{kvp.Key}] {v}"))) : "(No entities)" },
                { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
                { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" },
                { "KnowledgeBaseDocumentsContent", state.DomainsKnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var technicalAnalystOutput = await _technicalAnalystAgent.ExecuteAsync(new TechnicalAnalystAgentInput
            {
                Intent = state.CanonicalizedIntent,
                SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
                Entities = state.ClassifiedUserRequest.EntitiesByDomain,
                UserPreferences = state.ClassifiedUserRequest.UserPreferences,
                AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = string.Join("------" + Environment.NewLine, state.DomainsKnowledgeBaseDocumentsContent.Select(doc => doc.Print())),
                LanguageOfKnowledgeBase = _workflowConfiguration.LanguageOfKnowledgeBase
            }, cancellationToken);

            state.APISKnowledgeBaseQuery = technicalAnalystOutput.APISKnowledgeBaseQuery;
            state.AddTokenUsage(AgentMesh.Application.Configuration.TechnicalAnalystAgentConfiguration.AgentName, technicalAnalystOutput.InputTokenCount, technicalAnalystOutput.OutputTokenCount, stopwatch.Elapsed, "Technical Analyst Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "KnowledgeBaseAPIQueries", state.APISKnowledgeBaseQuery.Any() ? ToBulletList(state.APISKnowledgeBaseQuery) : "(No queries)" },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Technical Analyst Agent", notifyDictionary);
        }

        private async Task ExecuteCoderAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Coder Agent...");
            var businessRequirements = state.BusinessRequirements ?? "(No business requirements)";
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Coder Agent", new Dictionary<string, string>
            {
                { "BusinessRequirements", businessRequirements },
                { "KnowledgeBaseAPIDocuments", state.KnowledgeBaseAPIDocumentsContent.Any() ? ToBulletList(state.KnowledgeBaseAPIDocumentsContent.Select(doc => doc.File)) : "(No documents)" }
            });

            var coderAgentOutput = await _coderAgent.ExecuteAsync(new CoderAgentInput
            {
                BusinessRequirements = businessRequirements,
                KnowledgeBaseAPIDocumentsContent = state.KnowledgeBaseAPIDocumentsContent.Select(doc => new KnowledgeBaseGetDocsOutputItem
                {
                    File = doc.File,
                    Content = doc.Content
                })
            });
            state.GeneratedCode = coderAgentOutput.CodeToRun;
            state.AddTokenUsage(CoderAgentConfiguration.AgentName, coderAgentOutput.InputTokenCount, coderAgentOutput.OutputTokenCount, stopwatch.Elapsed, "Coder Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "CodeToRun", state.GeneratedCode },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Coder Agent", notifyDictionary);
        }


        private async Task ExecuteCodeFixerForRuntimeErrorsAsync(CodeModeWorkflowState state, string analysis, int iteration)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Code Fixer Agent for runtime errors... Iteration {Iteration}", iteration);
            await _workflowProgressNotifier.NotifyWorkflowStepStart($"Code Fixer Agent for Runtime Errors (Iteration {iteration})", new Dictionary<string, string>
            {
                { "CodeToFix", state.LastCodeWithLineNumbers ?? "(No code available)" },
                { "IssuesCount", "1" }
            });

            var codeFixerOutput = await _codeFixerAgent.ExecuteAsync(new CodeFixerAgentInput
            {
                CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty,
                Issues = [analysis]
            });
            state.GeneratedCode = codeFixerOutput.FixedCode;
            state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount, stopwatch.Elapsed, $"Code Fixer Agent for Runtime Errors (Iteration {iteration})");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "FixedCode", state.GeneratedCode },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd($"Code Fixer Agent for Runtime Errors (Iteration {iteration})", notifyDictionary);
        }

        private async Task<bool> ExecuteJSSandboxAsync(CodeModeWorkflowState state, bool isReexecution)
        {
            var stopwatch = Stopwatch.StartNew();
            var stepName = isReexecution ? "JS Sandbox Executor (Re-execution)" : "JS Sandbox Executor";
            var iterationMessage = isReexecution ? "again after code revision" : "";

            bool sandBoxError = false;
            try
            {
                _logger.LogDebug("Running JS Sandbox Executor {IterationMessage}", iterationMessage);
                await _workflowProgressNotifier.NotifyWorkflowStepStart(stepName, new Dictionary<string, string>
                {
                    { "Code", state.GeneratedCode ?? "(No generated code)" }
                });

                var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
                {
                    Code = state.GeneratedCode ?? string.Empty
                });
                state.SandboxResult = executionOutput.Result;
                state.SandboxExecutionId = executionOutput.ExecutionId;
                state.CodeExecutionResultType = SandboxResultType.Success;
                var notifyDictionary = new Dictionary<string, string>
                {
                    { "ExecutionId", state.SandboxExecutionId },
                    { "Result", state.SandboxResult },
                    { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
                };
                await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
            }
            catch (CodeSandboxCallException ex)
            {
                state.SandboxResult = ex.Message;
                state.CodeExecutionResultType = ex.ErrorType switch
                {
                    "CodeSyntaxError" => SandboxResultType.SyntaxError,
                    "InvalidRequest" => SandboxResultType.CallError,
                    _ => SandboxResultType.ApplicationError
                };
                sandBoxError = true;
                var notifyDictionary = new Dictionary<string, string>
                {
                    { "Error", state.SandboxResult },
                    { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
                };
                await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
            }
            catch (Exception ex)
            {
                state.SandboxResult = ex.Message;
                state.CodeExecutionResultType = SandboxResultType.ApplicationError;
                sandBoxError = true;
                var notifyDictionary = new Dictionary<string, string>
                {
                    { "Error", state.SandboxResult },
                    { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
                };
                await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
            }

            state.AddStepUsage(stepName, stopwatch.Elapsed, false);

            return sandBoxError;
        }

        private async Task<string> ExecuteCodeExecutionFailuresDetectorAsync(CodeModeWorkflowState state, int iteration)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Code Execution Failures Detector Agent... Iteration {Iteration}", iteration);
            await _workflowProgressNotifier.NotifyWorkflowStepStart($"Code Execution Failures Detector Agent (Iteration {iteration})", new Dictionary<string, string>
            {
                { "CodeWithLineNumbers", state.LastCodeWithLineNumbers ?? "(No code available)" },
                { "ExecutionResult", state.SandboxResult ?? "(No execution result)" }
            });

            var detectorOutput = await _codeExecutionFailuresDetectorAgent.ExecuteAsync(new CodeExecutionFailuresDetectorAgentInput
            {
                CodeWithLineNumbers = state.LastCodeWithLineNumbers ?? string.Empty,
                ExecutionResult = state.SandboxResult ?? string.Empty
            });
            state.CodeExecutionFailuresDetectorIterationCount++;
            state.AddTokenUsage(CodeExecutionFailuresDetectorAgentConfiguration.AgentName, detectorOutput.InputTokenCount, detectorOutput.OutputTokenCount, stopwatch.Elapsed, $"Code Execution Failures Detector Agent (Iteration {iteration})");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "Analysis", detectorOutput.Analysis },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd($"Code Execution Failures Detector Agent (Iteration {iteration})", notifyDictionary);

            return detectorOutput.Analysis;
        }

        private async Task ExecuteResultsPresenterAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Results Presenter Agent...");
            var originalUserRequest = state.OriginalUserRequest;
            var canonicalizedIntent = state.CanonicalizedIntent;

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Results Presenter Agent", new Dictionary<string, string>
            {
                { "Data", state.SandboxResult ?? "(No sandbox result)" },
                { "OriginalUserRequest", originalUserRequest },
                { "CanonicalizedIntent", canonicalizedIntent },
                { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
                { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
                { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" }
            });

            var resultsPresenterOutput = await _resultsPresenterAgent.ExecuteAsync(new ResultsPresenterAgentInput
            {
                Data = state.SandboxResult,
                OriginalUserRequest = originalUserRequest,
                CanonicalizedIntent = canonicalizedIntent,
                SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
                UserPreferences = state.ClassifiedUserRequest.UserPreferences,
                Memories = state.PastMemoriesQueryResults.Select(m => m.Memory)
            });
            state.PresenterOutput = resultsPresenterOutput.Content;
            state.AddTokenUsage(ResultsPresenterAgentConfiguration.AgentName, resultsPresenterOutput.InputTokenCount, resultsPresenterOutput.OutputTokenCount, stopwatch.Elapsed, "Results Presenter Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "Content", state.PresenterOutput },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Results Presenter Agent", notifyDictionary);
        }


        private async Task ExecuteDocumentationAgentAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Documentation Agent...");
            var enrichedUserRequest = state.CanonicalizedIntent;
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Documentation Agent", new Dictionary<string, string>
            {
                { "EnrichedUserRequest", enrichedUserRequest },
                { "Intent", state.CanonicalizedIntent },
                { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
                { "Entities", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(v => $"[{kvp.Key}] {v}"))) : "(No entities)" },
                { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
                { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" },
                { "DomainsKnowledgeBaseDocumentsContent", state.DomainsKnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var serializedDocumentation = SerializeDocumentation(state.DomainsKnowledgeBaseDocumentsContent);

            var output = await _documentationAgent.ExecuteAsync(new DocumentationAgentInput
            {
                EnrichedUserRequest = enrichedUserRequest,
                Intent = state.CanonicalizedIntent,
                SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
                Entities = state.ClassifiedUserRequest.EntitiesByDomain,
                UserPreferences = state.ClassifiedUserRequest.UserPreferences,
                AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = serializedDocumentation
            }, cancellationToken);
            state.DocumentationContent = output.Content;
            state.AddTokenUsage(DocumentationAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Documentation Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "Content", state.DocumentationContent ?? "(No documentation content)" },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Documentation Agent", notifyDictionary);
        }

        private static string SerializeDocumentation(IEnumerable<KnowledgeBaseDocumentContent> documents)
        {
            var serializedDocs = documents.Select(kv => $"{kv.Content}\n\nOriginal file: {kv.File}");
            return string.Join(Environment.NewLine + "---" + Environment.NewLine + "---", serializedDocs);
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
                if (state.CodeExecutionResultType == SandboxResultType.CallError)
                {
                    data = state.SandboxResult;
                }
                else
                {
                    data = state.PresenterOutput;
                }
            }

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Personal Assistant Agent", new Dictionary<string, string>
            {
                { "Data", data ?? "(No data)" },
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
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Personal Assistant Agent", notifyDictionary);
        }

        private static string GetElapsedTime(Stopwatch stopwatch) => GetElapsedTime(stopwatch.Elapsed);

        private static string GetElapsedTime(TimeSpan elapsed) => $"{elapsed.TotalMilliseconds:0}ms";

        private static string ToBulletList<T>(IEnumerable<T> items)
            => string.Join("\n", items.Select(item => $"- {item}"));

        private static string CreateKnowledgeBaseCacheLookupKey(string query, KnowledgeBaseQuerySearchType queryType)
            => $"{queryType}|{NormalizeCacheLookupValue(query)}";

        private static string NormalizeCacheLookupValue(string? value)
            => value?.Trim() ?? string.Empty;

        public string GetIngressExecutorName() => IntentExtractorAgentConfiguration.AgentName;

        public string GetEgressExecutorName() => PersonalAssistantAgentConfiguration.AgentName;

        [System.Text.RegularExpressions.GeneratedRegex(@"\[\[(.*?)\]\]")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();
    }
}

