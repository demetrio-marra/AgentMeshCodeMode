using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Models;
using AgentMesh.Models.CodeExecutionFailuresDetector;
using AgentMesh.Models.CodeFixer;
using AgentMesh.Models.Coder;
using AgentMesh.Models.CodeSandbox;
using AgentMesh.Models.Documentation;
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
using AgentMesh.Models.DomainExpert;
using AgentMesh.Models.TechnicalAnalyst;

namespace AgentMesh.Application.Workflows
{
    public partial class CodeModeWorkflow(ILogger<CodeModeWorkflow> logger,
        IWorkflowProgressNotifier workflowProgressNotifier,
        IDomainExpertAgent domainExpertAgent,
        ITechnicalAnalystAgent technicalAnalystAgent,
        IDocumentationAgent documentationAgent,
        ICoderAgent coderAgent,
        ICodeFixerAgent codeFixerAgent,
        ICodeExecutionFailuresDetectorAgent codeExecutionFailuresDetectorAgent,
        IResultsPresenterAgent resultsPresenterAgent,
        IJSSandboxExecutor jsSandboxExecutor,
        IIntentExtractorAgent intentExtractorAgent,
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

        private const bool AUTOMATICALLY_FETCH_RELATED_APIS_DOCUMENTATION = true;

        private readonly ILogger<CodeModeWorkflow> _logger = logger;
        private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;

        private readonly IDomainExpertAgent _domainExpertAgent = domainExpertAgent;
        private readonly ITechnicalAnalystAgent _technicalAnalystAgent = technicalAnalystAgent;
        private readonly IDocumentationAgent _documentationAgent = documentationAgent;
        private readonly ICoderAgent _coderAgent = coderAgent;
        private readonly ICodeFixerAgent _codeFixerAgent = codeFixerAgent;
        private readonly ICodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent = codeExecutionFailuresDetectorAgent;
        private readonly IResultsPresenterAgent _resultsPresenterAgent = resultsPresenterAgent;
        private readonly IJSSandboxExecutor _jsSandboxExecutor = jsSandboxExecutor;
        private readonly IIntentExtractorAgent _intentExtractorAgent = intentExtractorAgent;
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
                await ExecuteKnowledgeBaseServiceFastSearchAsync(state);
            }

            await ExecuteRequirementsCollectorAsync(state);


            if (_workflowConfiguration.EnableCacheService
                && (state.PastMemoriesQuery.Any()
                    || state.DomainsKnowledgeBaseQuery.Any()))
            {
                await ExecuteQueryCacheServiceAsync(state);
            }

            var memoryTask = (_workflowConfiguration.EnableMemoryService && state.PastMemoriesQuery.Any())
                ? ExecuteAgentMemoryServiceAsync(state)
                : Task.CompletedTask;

            var knowledgeBaseTask = state.DomainsKnowledgeBaseQuery.Any()
                ? ExecuteKnowledgeBaseServiceSearchAsync(state)
                : Task.CompletedTask;

            await Task.WhenAll(memoryTask, knowledgeBaseTask);

            if (state.DomainsKnowledgeBaseQueryResults.Results.Any())
            {
                await ExecuteKnowledgeBaseDocumentsExtractorAsync(state);
            }

            if (state.ClassifiedUserRequest.IntentCategory == UserIntentCategoryValues.Documentation)
            {
                await ExecuteDocumentationAgentAsync(state);
            }
            else if (state.ClassifiedUserRequest.IntentCategory == UserIntentCategoryValues.TaskExecution)
            {
                var domainExpertTask = ExecuteDomainExpertAsync(state);
                var technicalAnalystTask = ExecuteTechnicalAnalystAsync(state);

                await Task.WhenAll(domainExpertTask, technicalAnalystTask);

                if (state.APISKnowledgeBaseQuery.Any())
                {
                    await ExecuteKnowledgeBaseApiDocumentsExtractorAsync(state);
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
            else
            {
                throw new Exception($"Unknown user intent category: {state.ClassifiedUserRequest.IntentCategory}");
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

        private async Task ExecuteKnowledgeBaseDocumentsExtractorAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Knowledge Base Documents Extractor Service...");

            var fileNamesToExtract = state.DomainsKnowledgeBaseQueryResults.Results
                .Select(r => r.File)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct()
                .ToList();

            await _workflowProgressNotifier.NotifyWorkflowStepStart("KB Documents Extractor Service (Domain)", new Dictionary<string, string>
            {
                { "Documents", ToBulletList(fileNamesToExtract) }
            });

            var fetchedFilesContent = await _knowledgeBaseGetDocsExecutor.ExecuteAsync(new AgentMesh.Models.KnowledgeBase.KnowledgeBaseGetDocsInput
            {
                FilePaths = fileNamesToExtract
            });

            state.DomainsKnowledgeBaseDocumentsContent = [.. state.DomainsKnowledgeBaseQueryResults.Results
                .Join(fetchedFilesContent.Results, kb => kb.File, fc => fc.File, (kb, fc) => new { kb, fc })
                .Select(kb => new KnowledgeBaseDocumentContent
                {
                    File = kb.kb.File,
                    Content = kb.fc.Content
                })];

            state.AddStepUsage("KB Documents Extractor Service (Domain)", stopwatch.Elapsed, false);

            var notifyDictionary = new Dictionary<string, string>
            {
                { "Total files extracted", state.DomainsKnowledgeBaseDocumentsContent.Count().ToString() },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("KB Documents Extractor Service (Domain)", notifyDictionary);
        }

        private async Task ExecuteKnowledgeBaseApiDocumentsExtractorAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Knowledge Base API Documents Extractor Service...");

            await _workflowProgressNotifier.NotifyWorkflowStepStart("KB Documents Extractor Service (APIs)", new Dictionary<string, string>
            {
                { "Queries", ToBulletList(state.APISKnowledgeBaseQuery) }
            });

            var apiKnowledgeBaseQueryResults = await _knowledgeBaseSearchExecutor.ExecuteAsync(new KnowledgeBaseQueryInput
            {
                Collections = [APIS_DOCUMENTATION_COLLECTION_NAME],
                UserIntent = state.ClassifiedUserRequest.Intent,
                Queries = state.APISKnowledgeBaseQuery.ToList()
            }, CancellationToken.None);

            var apiFilePaths = apiKnowledgeBaseQueryResults.Results
                .Select(result => NormalizeKnowledgeBaseDocumentKey(result.File))
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var fetchedFilesContent = await _knowledgeBaseGetDocsExecutor.ExecuteAsync(new AgentMesh.Models.KnowledgeBase.KnowledgeBaseGetDocsInput
            {
                FilePaths = apiFilePaths
            });

            var apiDocumentsByFile = fetchedFilesContent.Results
                .Select(doc => new
                {
                    Key = NormalizeKnowledgeBaseDocumentKey(doc.File),
                    Document = doc
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => new KnowledgeBaseDocumentContent
                    {
                        File = group.Key,
                        Content = group.First().Document.Content
                    },
                    StringComparer.OrdinalIgnoreCase);

            if (AUTOMATICALLY_FETCH_RELATED_APIS_DOCUMENTATION)
            {
                var pendingFilesToFetch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var document in apiDocumentsByFile.Values)
                {
                    foreach (var linkedFile in ExtractLinkedDocumentPaths(document.File, document.Content))
                    {
                        if (!apiDocumentsByFile.ContainsKey(linkedFile))
                        {
                            pendingFilesToFetch.Add(linkedFile);
                        }
                    }
                }

                while (pendingFilesToFetch.Any())
                {
                    var filesToFetch = pendingFilesToFetch.ToList();
                    pendingFilesToFetch.Clear();

                    var linkedFilesContent = await _knowledgeBaseGetDocsExecutor.ExecuteAsync(new KnowledgeBaseGetDocsInput
                    {
                        FilePaths = filesToFetch
                    });

                    var newlyLoadedDocuments = linkedFilesContent.Results
                        .Select(doc => new
                        {
                            Key = NormalizeKnowledgeBaseDocumentKey(doc.File),
                            Document = doc
                        })
                        .Where(item => !string.IsNullOrWhiteSpace(item.Key) && !apiDocumentsByFile.ContainsKey(item.Key))
                        .Select(item => new KnowledgeBaseDocumentContent
                        {
                            File = item.Key,
                            Content = item.Document.Content
                        })
                        .ToList();

                    foreach (var loadedDocument in newlyLoadedDocuments)
                    {
                        apiDocumentsByFile[loadedDocument.File] = loadedDocument;
                    }

                    foreach (var loadedDocument in newlyLoadedDocuments)
                    {
                        foreach (var linkedFile in ExtractLinkedDocumentPaths(loadedDocument.File, loadedDocument.Content))
                        {
                            if (!apiDocumentsByFile.ContainsKey(linkedFile))
                            {
                                pendingFilesToFetch.Add(linkedFile);
                            }
                        }
                    }
                }
            }

            state.KnowledgeBaseAPIDocumentsContent = [.. apiDocumentsByFile.Values];

            state.AddStepUsage("KB Documents Extractor Service (APIs)", stopwatch.Elapsed, false);

            var notifyDictionary = new Dictionary<string, string>
            {
                { "Total files extracted", state.KnowledgeBaseAPIDocumentsContent.Count().ToString() },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("KB Documents Extractor Service (APIs)", notifyDictionary);
        }

        private async Task ExecuteIntentExtractorAsync(CodeModeWorkflowState state, IEnumerable<ContextMessage> chatHistory)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Intent Extractor Agent...");

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Intent Extractor Agent", new Dictionary<string, string>
            {
                { "ContextMessages", "<omitted for brevity>. Total: " + chatHistory.Count().ToString() },
                { "UserLastRequest", state.OriginalUserRequest }
            });

            var intentExtractorOutput = await _intentExtractorAgent.ExecuteAsync(new IntentExtractorAgentInput
            {
                ContextMessages = [.. state.InitialContextMessages],
                UserLastRequest = state.OriginalUserRequest,
                ApplicationDomainList = _workflowConfiguration.ApplicationDomainList
            });

            state.ClassifiedUserRequest = new StructuredUserRequest
            {
                Intent = intentExtractorOutput.UserIntent,
                IntentCategory = intentExtractorOutput.UserIntentCategory,
                LanguageOfTheUser = intentExtractorOutput.LanguageOfTheUser,
                EntitiesByDomain = intentExtractorOutput.EntitiesByDomain,
                SupportingIntentInformation = intentExtractorOutput.SupportingIntentInformation,
                UserPreferences = intentExtractorOutput.UserPreferences,
                MissingMemories = intentExtractorOutput.MissingMemories
            };

            state.AddTokenUsage(IntentExtractorAgentConfiguration.AgentName, intentExtractorOutput.InputTokenCount, intentExtractorOutput.OutputTokenCount, stopwatch.Elapsed, "Intent Extractor Agent");

            var notifyDictionary = new Dictionary<string, string>
            {
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

        private async Task ExecuteRequirementsCollectorAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Requirements Collector Agent...");

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Requirements Collector Agent", new Dictionary<string, string>
            {
                { "UserIntent", state.ClassifiedUserRequest.Intent ?? "(No intent extracted)" },
                { "UserIntentCategory", state.ClassifiedUserRequest.IntentCategory.ToString() },
                { "EntitiesByDomain", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(e => $"[{kvp.Key}] {e}"))) : "(No entities)" },
                { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
                { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
                { "MissingMemories", state.ClassifiedUserRequest.MissingMemories.Any() ? ToBulletList(state.ClassifiedUserRequest.MissingMemories) : "(No missing memories)" },
                { "FastKnowledgeBaseResults", state.FastKnowledgeBaseQueryResults.Results.Any() ? ToBulletList(state.FastKnowledgeBaseQueryResults.Results.Select(r => $"[{r.File}] {r.Title}")) : "(No fast knowledge base results)" }
            });

            var output = await _requirementsCollectorAgent.ExecuteAsync(new RequirementsCollectorAgentInput
            {
                UserIntent = state.ClassifiedUserRequest.Intent ?? string.Empty,
                UserIntentCategory = state.ClassifiedUserRequest.IntentCategory,
                EntitiesByDomain = state.ClassifiedUserRequest.EntitiesByDomain,
                SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
                UserPreferences = state.ClassifiedUserRequest.UserPreferences,
                MissingMemories = state.ClassifiedUserRequest.MissingMemories,
                FastKnowledgeBaseQueryResults = state.FastKnowledgeBaseQueryResults.Results
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

        private async Task ExecuteQueryCacheServiceAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Queries Cache Service...");

            var notifyInputDictionary = new Dictionary<string, string>();
            if (state.PastMemoriesQuery.Any())
            {
                notifyInputDictionary.Add("MissingPastMemories", ToBulletList(state.PastMemoriesQuery));
            }
            if (state.DomainsKnowledgeBaseQuery.Any())
            {
                notifyInputDictionary.Add("MissingKnowledgeBaseEntries", ToBulletList(state.DomainsKnowledgeBaseQuery));
            }
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Queries Cache Service", notifyInputDictionary);

            var originalMemoryQueries = state.PastMemoriesQuery.ToList();
            var originalKnowledgeBaseQueries = state.DomainsKnowledgeBaseQuery.ToList();

            var totalTokensForEmbedding = 0;

            if (originalMemoryQueries.Any())
            {
                var memoryQueries = originalMemoryQueries
                    .Select(query => new AgentMemoryQueriesCacheItemInput { Query = query })
                    .ToList();

                var cachedMemoryResult = await _queriesCacheService.GetMemoryCachedItemsAsync(memoryQueries);
                totalTokensForEmbedding += cachedMemoryResult.TotalTokens;

                var cachedMemoryItemsList = cachedMemoryResult.Items.ToList();

                if (cachedMemoryItemsList.Any())
                {
                    state.PastMemoriesQueryResults = cachedMemoryItemsList
                        .Select(item => new AgentMemoryQueryResultItem
                        {
                            Memory = item.Result,
                            Confidence = item.Relevance
                        })
                        .Distinct()
                        .ToList();

                    var cachedQueries = cachedMemoryItemsList
                        .Select(item => NormalizeCacheLookupValue(item.SearchedQuery))
                        .Where(query => !string.IsNullOrWhiteSpace(query))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    state.PastMemoriesQuery = originalMemoryQueries.Where(q => !cachedQueries.Contains(NormalizeCacheLookupValue(q)));
                }
            }

            if (originalKnowledgeBaseQueries.Any())
            {
                var knowledgeBaseQueries = originalKnowledgeBaseQueries
                    .Where(k => k.SearchType != KnowledgeBaseQuerySearchType.Keyword) // ALWAYS Exclude keyword search queries from caching
                    .ToList();

                var cachedKnowledgeBaseResult = await _queriesCacheService.GetKnowledgeBaseCachedItemsAsync(knowledgeBaseQueries);
                totalTokensForEmbedding += cachedKnowledgeBaseResult.TotalTokens;

                var cachedKnowledgeBaseItemsList = cachedKnowledgeBaseResult.Items.ToList();

                if (cachedKnowledgeBaseItemsList.Any())
                {
                    state.DomainsKnowledgeBaseQueryResults = new KnowledgeBaseQueryResult
                    {
                        Results = cachedKnowledgeBaseItemsList
                            .Select(item => new KnowledgeBaseQueryResultItem
                            {
                                Id = item.DocumentId,
                                File = item.DocumentFile,
                                Title = item.DocumentTitle,
                                Summary = item.DocumentSummary,
                                Relevance = item.Relevance
                            })
                            .Distinct()
                            .ToList()
                    };

                    var cachedQueryKeys = cachedKnowledgeBaseItemsList
                        .Select(item => CreateKnowledgeBaseCacheLookupKey(item.SearchedQuery, item.SearchedQueryType))
                        .Where(key => !string.IsNullOrWhiteSpace(key))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    state.DomainsKnowledgeBaseQuery = originalKnowledgeBaseQueries
                        .Where(entry => !cachedQueryKeys.Contains(CreateKnowledgeBaseCacheLookupKey(entry.Query, entry.SearchType)));
                }
            }

            var tokenUsageInfo = new AgentTokenUsageEntry
            {
                AgentName = "Embedding Service",
                InputTokens = totalTokensForEmbedding,
                OutputTokens = 0
            };
            state.AddStepUsage("Queries Cache Service", stopwatch.Elapsed, true, tokenUsageInfo);

            var notifyDictionary = new Dictionary<string, string>
            {
                { "MemoryCacheHitsCount", (originalMemoryQueries.Count - state.PastMemoriesQuery.Count()).ToString() },
                { "MemoryRemainingQueriesCount", state.PastMemoriesQuery.Count().ToString() },
                { "KnowledgeBaseCacheHitsCount", (originalKnowledgeBaseQueries.Count - state.DomainsKnowledgeBaseQuery.Count()).ToString() },
                { "KnowledgeBaseRemainingQueriesCount", state.DomainsKnowledgeBaseQuery.Count().ToString() },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Queries Cache Service", notifyDictionary);
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


        private async Task ExecuteKnowledgeBaseServiceSearchAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Knowledge Base Service...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("KB Search Service", new Dictionary<string, string>
            {
                { "MissingKnowledgeBaseEntries", ToBulletList(state.DomainsKnowledgeBaseQuery) }
            });

            var queriesList = state.DomainsKnowledgeBaseQuery.ToList();

            KnowledgeBaseQueryInput queryInput = new()
            {
                Collections = [DOMAINS_DOCUMENTATION_COLLECTION_NAME],
                UserIntent = state.ClassifiedUserRequest.Intent,
                Queries = queriesList
            };

            var brcOutput = await _knowledgeBaseSearchExecutor.ExecuteAsync(queryInput, CancellationToken.None);

            var existingResults = state.DomainsKnowledgeBaseQueryResults.Results.ToList();
            state.DomainsKnowledgeBaseQueryResults = new KnowledgeBaseQueryResult
            {
                Results = existingResults.Concat(brcOutput.Results).ToList()
            };

            if (_workflowConfiguration.EnableCacheService && brcOutput.Results.Any())
            {
                var cacheableQueries = queriesList
                    .Where(entry => entry.SearchType != KnowledgeBaseQuerySearchType.Keyword)
                    .ToList();

                if (cacheableQueries.Any())
                {
                    var cacheItems = new List<KnowledgeBaseQueriesCacheItem>();
                    foreach (var query in cacheableQueries)
                    {
                        foreach (var result in brcOutput.Results)
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

                    var cacheUpdateResult = await _queriesCacheService.SetKnowledgeBaseCachedItemsAsync(cacheItems);
                    
                    var tokenUsageInfo = new AgentTokenUsageEntry
                    {
                        AgentName = "Query Cache Updater Service (Knowledge)",
                        InputTokens = cacheUpdateResult.TotalTokens,
                        OutputTokens = 0
                    };
                    state.AddStepUsage("KB Search Service", stopwatch.Elapsed, true, tokenUsageInfo);
                }
                else
                {
                    state.AddStepUsage("KB Search Service", stopwatch.Elapsed, false);
                }
            }
            else
            {
                state.AddStepUsage("KB Search Service", stopwatch.Elapsed, false);
            }

            var notifyDictionary = new Dictionary<string, string>
            {
                { "ExtractedKnowledgeBaseEntries", ToBulletList(brcOutput.Results.Select(m => $"File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")) },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("KB Search Service", notifyDictionary);
        }

        private async Task ExecuteKnowledgeBaseServiceFastSearchAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Knowledge Base Fast Service...");
            
            var domainsDisplay = ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.Select(kvp =>
                $"{kvp.Key}: {string.Join(", ", kvp.Value)}"));
            
            await _workflowProgressNotifier.NotifyWorkflowStepStart("KB Fast Search Service", new Dictionary<string, string>
            {
                { "Domains", domainsDisplay }
            });

            var queries = new List<KnowledgeBaseQueryInputItem>();
            
            foreach (var domainEntry in state.ClassifiedUserRequest.EntitiesByDomain)
            {
                var domain = domainEntry.Key;
                var entities = domainEntry.Value;
                // Add a keyword search for the domain
                queries.Add(new KnowledgeBaseQueryInputItem
                {
                    Query = domain,
                    SearchType = KnowledgeBaseQuerySearchType.Keyword,
                });
                // Add a keyword search for each entity in the domain
                foreach (var entity in entities)
                {
                    queries.Add(new KnowledgeBaseQueryInputItem
                    {
                        Query = entity,
                        SearchType = KnowledgeBaseQuerySearchType.Keyword
                    });
                }
            }

            if (!queries.Any())
            {
                _logger.LogDebug("No domains or entities to search for in knowledge base");
                state.AddStepUsage("KB Fast Search Service", stopwatch.Elapsed, false);
                await _workflowProgressNotifier.NotifyWorkflowStepEnd("KB Fast Search Service", new Dictionary<string, string>
                {
                    { "ExtractedKnowledgeBaseEntries", "(No queries generated)" },
                    { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
                });
                return;
            }

            KnowledgeBaseQueryInput queryInput = new()
            {
                UserIntent = state.ClassifiedUserRequest.Intent,
                Queries = queries,
                Collections = [DOMAINS_DOCUMENTATION_COLLECTION_NAME]
            };

            var brcOutput = await _knowledgeBaseSearchFastExecutor.ExecuteAsync(queryInput, CancellationToken.None);

            state.FastKnowledgeBaseQueryResults = new KnowledgeBaseQueryResult
            {
                Results = brcOutput.Results.ToList()
            };

            state.AddStepUsage("KB Fast Search Service", stopwatch.Elapsed, false);

            var notifyDictionary = new Dictionary<string, string>
            {
                { "FastKnowledgeBaseQueryResults", ToBulletList(state.FastKnowledgeBaseQueryResults.Results.Select(m => $"File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")) },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("KB Fast Search Service", notifyDictionary);
        }


        private async Task ExecuteDomainExpertAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Domain Expert Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Domain Expert Agent", new Dictionary<string, string>
            {
                { "Intent", state.ClassifiedUserRequest.Intent ?? "(No intent)" },
                { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
                { "Entities", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(v => $"[{kvp.Key}] {v}"))) : "(No entities)" },
                { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
                { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" },
                { "KnowledgeBaseDocumentsContent", state.DomainsKnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var domainExpertOutput = await _domainExpertAgent.ExecuteAsync(new DomainExpertAgentInput
            {
                Intent = state.ClassifiedUserRequest.Intent ?? string.Empty,
                SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
                Entities = state.ClassifiedUserRequest.EntitiesByDomain,
                UserPreferences = state.ClassifiedUserRequest.UserPreferences,
                AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = string.Join("------" + Environment.NewLine, state.DomainsKnowledgeBaseDocumentsContent.Select(doc => doc.Print()))
            }, cancellationToken);

            state.ShouldEngageCoder = true;
            state.BusinessRequirements = domainExpertOutput.BusinessRequirements;
            state.AddTokenUsage(DomainExpertAgentConfiguration.AgentName, domainExpertOutput.InputTokenCount, domainExpertOutput.OutputTokenCount, stopwatch.Elapsed, "Domain Expert Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "BusinessRequirements", state.BusinessRequirements ?? "(No business requirements)" },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Domain Expert Agent", notifyDictionary);
        }

        private async Task ExecuteTechnicalAnalystAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Technical Analyst Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Technical Analyst Agent", new Dictionary<string, string>
            {
                { "Intent", state.ClassifiedUserRequest.Intent ?? "(No intent)" },
                { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
                { "Entities", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(v => $"[{kvp.Key}] {v}"))) : "(No entities)" },
                { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
                { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" },
                { "KnowledgeBaseDocumentsContent", state.DomainsKnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var technicalAnalystOutput = await _technicalAnalystAgent.ExecuteAsync(new TechnicalAnalystAgentInput
            {
                Intent = state.ClassifiedUserRequest.Intent ?? string.Empty,
                SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
                Entities = state.ClassifiedUserRequest.EntitiesByDomain,
                UserPreferences = state.ClassifiedUserRequest.UserPreferences,
                AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = string.Join("------" + Environment.NewLine, state.DomainsKnowledgeBaseDocumentsContent.Select(doc => doc.Print()))
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
            var sandboxResult = state.SandboxResult ?? "(No sandbox result)";
            var enrichedUserRequest = state.ClassifiedUserRequest.Intent ?? "(No enriched user request)";
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Results Presenter Agent", new Dictionary<string, string>
            {
                { "Data", sandboxResult },
                { "EnrichedUserRequest", enrichedUserRequest }
            });

            var resultsPresenterOutput = await _resultsPresenterAgent.ExecuteAsync(new ResultsPresenterAgentInput
            {
                Data = sandboxResult,
                EnrichedUserRequest = enrichedUserRequest
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
            var enrichedUserRequest = state.ClassifiedUserRequest.Intent ?? "(No enriched user request)";
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Documentation Agent", new Dictionary<string, string>
            {
                { "EnrichedUserRequest", enrichedUserRequest },
                { "Intent", state.ClassifiedUserRequest.Intent ?? "(No intent)" },
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
                Intent = state.ClassifiedUserRequest.Intent ?? string.Empty,
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


        private static IEnumerable<string> ExtractLinkedDocumentPaths(string sourceFilePath, string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return [];
            }

            var links = MyRegex()
                .Matches(content)
                .Select(match => match.Groups[1].Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(link => ResolveLinkedDocumentPath(sourceFilePath, link))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return links;
        }

        private static string ResolveLinkedDocumentPath(string sourceFilePath, string rawLink)
        {
            var cleaned = rawLink.Trim();

            var anchorIndex = cleaned.IndexOf('#');
            if (anchorIndex >= 0)
            {
                cleaned = cleaned[..anchorIndex];
            }

            var queryStringIndex = cleaned.IndexOf('?');
            if (queryStringIndex >= 0)
            {
                cleaned = cleaned[..queryStringIndex];
            }

            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return string.Empty;
            }

            if (cleaned.Contains("://", StringComparison.OrdinalIgnoreCase)
                || cleaned.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            var normalizedLink = cleaned.Replace('\\', '/');

            if (normalizedLink.StartsWith("/"))
            {
                return NormalizeKnowledgeBasePath(normalizedLink.TrimStart('/'));
            }

            if (normalizedLink.StartsWith("./", StringComparison.Ordinal)
                || normalizedLink.StartsWith("../", StringComparison.Ordinal))
            {
                var normalizedSource = sourceFilePath.Replace('\\', '/');
                var sourceLastSlash = normalizedSource.LastIndexOf('/');
                var sourceDirectory = sourceLastSlash >= 0 ? normalizedSource[..sourceLastSlash] : string.Empty;
                var combined = string.IsNullOrWhiteSpace(sourceDirectory)
                    ? normalizedLink
                    : $"{sourceDirectory}/{normalizedLink}";

                return NormalizeKnowledgeBasePath(combined);
            }

            return NormalizeKnowledgeBasePath(normalizedLink);
        }

        private static string NormalizeKnowledgeBaseDocumentKey(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var normalized = path.Trim().Replace('\\', '/');
            if (normalized.StartsWith('/'))
            {
                normalized = normalized.TrimStart('/');
            }

            return NormalizeKnowledgeBasePath(normalized);
        }

        private static string NormalizeKnowledgeBasePath(string path)
        {
            var segments = path
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var normalizedSegments = new List<string>();
            foreach (var segment in segments)
            {
                if (segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (normalizedSegments.Count > 0)
                    {
                        normalizedSegments.RemoveAt(normalizedSegments.Count - 1);
                    }
                    continue;
                }

                normalizedSegments.Add(segment);
            }

            return string.Join("/", normalizedSegments);
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
            var enrichedUserRequest = state.ClassifiedUserRequest.Intent ?? "(No enriched user request)";

            string? data = null;
            if (state.ClassifiedUserRequest.IntentCategory == UserIntentCategoryValues.Documentation)
            {
                data = state.DocumentationContent;
            }
            else if (state.ClassifiedUserRequest.IntentCategory == UserIntentCategoryValues.TaskExecution)
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
                { "EnrichedUserRequest", enrichedUserRequest },
                { "LanguageOfTheUser", state.ClassifiedUserRequest.LanguageOfTheUser ?? "(No language specified)" }
            });

            var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
            {
                Data = data,
                EnrichedUserRequest = enrichedUserRequest,
                LanguageOfTheUser = state.ClassifiedUserRequest.LanguageOfTheUser
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

        private static string GetElapsedTime(Stopwatch stopwatch) => $"{stopwatch.ElapsedMilliseconds}ms";

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

