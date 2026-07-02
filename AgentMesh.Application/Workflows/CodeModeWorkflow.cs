using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Models;
using AgentMesh.Models.BusinessAdvisor;
using AgentMesh.Models.TechnicalAnalyst;
using AgentMesh.Models.CodeExecutionFailuresDetector;
using AgentMesh.Models.CodeFixer;
using AgentMesh.Models.Coder;
using AgentMesh.Models.CodeSandbox;
using AgentMesh.Models.CodeStaticAnalyzer;
using AgentMesh.Models.ContextAnalyzer;
using AgentMesh.Models.Documentation;
using AgentMesh.Models.IntentExtractor;
using AgentMesh.Models.PersonalAssistant;
using AgentMesh.Models.ResultsPresenter;
using static AgentMesh.Models.ContextAnalyzer.ContextAnalyzerAgentOutput;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using AgentMesh.Application.Helpers;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RelevantFactsEvaluator;
using System.Diagnostics;
using AgentMesh.Models.AgentMemory;
using System.Data;
using AgentMesh.Models.QueriesCache;

namespace AgentMesh.Application.Workflows
{
    public partial class CodeModeWorkflow(ILogger<CodeModeWorkflow> logger,
        IWorkflowProgressNotifier workflowProgressNotifier,
        ITechnicalAnalystAgent technicalAnalystAgent,
        IBusinessAdvisorAgent businessAdvisorAgent,
        IDocumentationAgent documentationAgent,
        ICoderAgent coderAgent,
        ICodeStaticAnalyzerAgent codeStaticAnalyzer,
        ICodeFixerAgent codeFixerAgent,
        ICodeExecutionFailuresDetectorAgent codeExecutionFailuresDetectorAgent,
        IResultsPresenterAgent resultsPresenterAgent,
        IJSSandboxExecutor jsSandboxExecutor,
        IIntentExtractorAgent intentExtractorAgent,
        IPersonalAssistantAgent personalAssistantAgent,
        IContextAnalyzerAgent contextAnalyzerAgent,
        IAgentMemoryRetriever agentMemoryRetriever,
        IAgentMemorySaver agentMemorySaver,
        IKnowledgeBaseSearchExecutor knowledgeBaseSearchExecutor,
        IKnowledgeBaseGetDocsExecutor knowledgeBaseGetDocsExecutor,
        IRelevantFactsEvaluatorAgent relevantFactsEvaluatorAgent,
        CodeModeWorkflowConfiguration workflowConfiguration,
        IQueriesCacheService queriesCacheService) : IWorkflow
    {
        private const string DOCUMENTATION_FOR_BUSINESSANALYST_SECTIONTITLE = "Documentation";
        private const string DOCUMENTATION_FOR_DEVELOPER_SECTIONTITLE = "Technical reference";
        private const string DOCUMENTATION_COLLECTION_NAME = "apis";
        private const bool AUTOMATICALLY_FETCH_RELATED_DOCUMENTATION = true;

        private const string KEYWORDS_SEARCH_TYPE = "lex";
        private const string SEMANTIC_SEARCH_TYPE = "vec";
        private const string HYPOTHETICAL_SEARCH_TYPE = "hyde";

        private readonly ILogger<CodeModeWorkflow> _logger = logger;
        private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;

        private readonly ITechnicalAnalystAgent _technicalAnalystAgent = technicalAnalystAgent;
        private readonly IBusinessAdvisorAgent _businessAdvisorAgent = businessAdvisorAgent;
        private readonly IDocumentationAgent _documentationAgent = documentationAgent;
        private readonly ICoderAgent _coderAgent = coderAgent;
        private readonly ICodeStaticAnalyzerAgent _codeStaticAnalyzer = codeStaticAnalyzer;
        private readonly ICodeFixerAgent _codeFixerAgent = codeFixerAgent;
        private readonly ICodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent = codeExecutionFailuresDetectorAgent;
        private readonly IResultsPresenterAgent _resultsPresenterAgent = resultsPresenterAgent;
        private readonly IJSSandboxExecutor _jsSandboxExecutor = jsSandboxExecutor;
        private readonly IIntentExtractorAgent _intentExtractorAgent = intentExtractorAgent;
        private readonly IPersonalAssistantAgent _personalAssistantAgent = personalAssistantAgent;
        private readonly IContextAnalyzerAgent _contextAnalyzerAgent = contextAnalyzerAgent;
        private readonly IAgentMemoryRetriever _agentMemoryRetriever = agentMemoryRetriever;
        private readonly IAgentMemorySaver _agentMemorySaver = agentMemorySaver;
        private readonly IKnowledgeBaseSearchExecutor _knowledgeBaseSearchExecutor = knowledgeBaseSearchExecutor;
        private readonly IKnowledgeBaseGetDocsExecutor _knowledgeBaseGetDocsExecutor = knowledgeBaseGetDocsExecutor;
        private readonly IRelevantFactsEvaluatorAgent _relevantFactsEvaluatorAgent = relevantFactsEvaluatorAgent;
        private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;
        private readonly IQueriesCacheService _queriesCacheService = queriesCacheService;

        public async Task<WorkflowResult> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory)
        {
            await _workflowProgressNotifier.NotifyWorkflowStart();

            var state = new CodeModeWorkflowState(userInput, chatHistory);

            await ExecuteIntentExtractorAsync(state, chatHistory);

            if (_workflowConfiguration.EnableCacheService
                && (state.MissingPastMemories.Any()
                    || state.MissingKnowledgeBaseSearchEntries.Any()))
            {
                await ExecuteQueryCacheServiceAsync(state);
            }

            if (_workflowConfiguration.EnableMemoryService && state.MissingPastMemories.Any())
            {
                await ExecuteAgentMemoryServiceAsync(state);
            }

            if (state.MissingKnowledgeBaseSearchEntries.Any())
            {
                await ExecuteKnowledgeBaseServiceSearchAsync(state);
            }

            await ExecuteContextAnalyzerAsync(state);

            if (state.UserIntentCategoryValue == UserIntentCategoryValues.Other)
            {
                goto CompleteWorkflow;
            }

            if (state.RelevantKnowledgeBaseFileNames.Any())
            {
                await ExecuteKnowledgeBaseDocumentsExtractorAsync(state);
            }

            if (state.UserIntentCategoryValue == UserIntentCategoryValues.TaskExecution)
            {
                await ExecuteTechnicalAnalystAsync(state);
                await ExecuteCoderAsync(state);
                await ExecuteCodeStaticAnalyzerAsync(state);

                for (int i = 0; i < 2 && !state.IsCodeValid && state.CodeIssues.Count != 0; i++)
                {
                    await ExecuteCodeFixerAsync(state, i + 1, false);
                    await ExecuteCodeStaticAnalyzerAsync(state);
                }

                await ExecuteJSSandboxAsync(state, false);

                if (state.CodeExecutionResultType == SandboxResultType.CallError)
                {
                    await CompleteWorkflowAsync(state, state.SandboxResult);
                }
                else if (state.CodeExecutionResultType == SandboxResultType.ApplicationError ||
                         state.CodeExecutionResultType == SandboxResultType.SyntaxError)
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
                    await CompleteWorkflowAsync(state, state.PresenterOutput);
                }
                else
                {
                    await ExecuteResultsPresenterAsync(state);
                    await CompleteWorkflowAsync(state, state.PresenterOutput);
                }
                goto WorkflowEnd;
            }
            else if (state.UserIntentCategoryValue == UserIntentCategoryValues.BusinessAdvisor)
            {
                await ExecuteBusinessAdvisorAsync(state);
                await CompleteWorkflowAsync(state, state.BusinessAdvisorContent);
                goto WorkflowEnd;
            }
            else if (state.UserIntentCategoryValue == UserIntentCategoryValues.Documentation)
            {
                await ExecuteDocumentationAsync(state);
                await CompleteWorkflowAsync(state, state.DocumentationContent);
                goto WorkflowEnd;
            }
            else
            {
                throw new Exception($"Unknown user intent category: {state.UserIntentCategoryValue}");
            }

        CompleteWorkflow:
            await CompleteWorkflowAsync(state);

        WorkflowEnd:

            if (_workflowConfiguration.EnableMemoryService)
            {
                var isWorthSaving = await ExecuteRelevantFactsEvaluatorAsync(state);
                if (isWorthSaving)
                {
                    await ExecuteAgentMemorySaverAsync(state);
                }
            }

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

            await _workflowProgressNotifier.NotifyWorkflowStepStart("KB Documents Extractor Service", new Dictionary<string, string>
            {
                { "Documents", string.Join("\n", state.RelevantKnowledgeBaseFileNames.Select(s => $"- {s}")) }
            });

            var fetchedFilesContent = await _knowledgeBaseGetDocsExecutor.ExecuteAsync(new AgentMesh.Models.KnowledgeBase.KnowledgeBaseGetDocsInput
            {
                FilePaths = state.RelevantKnowledgeBaseFileNames
            });

            // additional feature
            if (AUTOMATICALLY_FETCH_RELATED_DOCUMENTATION)
            {
                var relatedDocs = new List<string>();
                foreach (var mainDoc in fetchedFilesContent.Results)
                {
                    // find words within double square brackets [[...]] in the mainDoc.Content using regex
                    var matches = MyRegex().Matches(mainDoc.Content);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        var relatedDocName = match.Groups[1].Value;
                        if (!relatedDocs.Contains(relatedDocName))
                        {
                            relatedDocs.Add(relatedDocName);
                        }
                    }
                }

                // distinct on relatedDocs
                relatedDocs = [.. relatedDocs.Distinct()];

                // avoid extracting already fetched docs
                relatedDocs = [.. relatedDocs.Except(fetchedFilesContent.Results.Where(r => !string.IsNullOrEmpty(r.File)).Select(r => r.File!))];

                // fetch again the content of the related docs and add them to the fetchedFilesContent.Results
                if (relatedDocs.Count != 0)
                {
                    var relatedDocsContent = await _knowledgeBaseGetDocsExecutor.ExecuteAsync(new AgentMesh.Models.KnowledgeBase.KnowledgeBaseGetDocsInput
                    {
                        FilePaths = relatedDocs
                    });
                    fetchedFilesContent.Results = fetchedFilesContent.Results.Concat(relatedDocsContent.Results);
                }
            }

            state.KnowledgeBaseDocumentsContent = [.. state.KnowledgeBaseQueryResults.Results
                .Join(fetchedFilesContent.Results, kb => kb.File, fc => fc.File, (kb, fc) => new { kb, fc })
                .Select(kb => new KnowledgeBaseDocumentContent
                {
                    File = kb.kb.File,
                    Content = kb.fc.Content
                })];

            state.AddStepUsage("KB Documents Extractor Service", stopwatch.Elapsed, false);

            var notifyDictionary = new Dictionary<string, string>
            {
                { "Total files extracted", state.KnowledgeBaseDocumentsContent.Count().ToString() },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("KB Documents Extractor Service", notifyDictionary);
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
                UserLastRequest = state.OriginalUserRequest
            });

            state.UserIntent = intentExtractorOutput.UserIntent;
            state.MissingPastMemories = intentExtractorOutput.MissingPastMemories;
            state.MissingKnowledgeBaseSearchEntries = intentExtractorOutput.MissingKnowledgeBaseSearchEntries;
            state.LanguageOfTheUser = intentExtractorOutput.LanguageOfTheUser;

            state.AddTokenUsage(IntentExtractorAgentConfiguration.AgentName, intentExtractorOutput.InputTokenCount, intentExtractorOutput.OutputTokenCount, stopwatch.Elapsed, "Intent Extractor Agent");

            var notifyDictionary = new Dictionary<string, string>
            {
                { "ExtractedIntent", state.UserIntent ?? "(No intent extracted)" }
            };
            if (state.LanguageOfTheUser != null)
            {
                notifyDictionary.Add("LanguageOfTheUser", state.LanguageOfTheUser);
            }
            if (state.MissingPastMemories != null && state.MissingPastMemories.Any())
            {
                notifyDictionary.Add("MissingPastMemoriesDetails", string.Join("\n", state.MissingPastMemories.Select(m => $"- {m}")));
            }
            if (state.MissingKnowledgeBaseSearchEntries != null && state.MissingKnowledgeBaseSearchEntries.Any())
            {
                notifyDictionary.Add("MissingKnowledgeBaseEntriesDetails", string.Join("\n", state.MissingKnowledgeBaseSearchEntries.Select(m => $"- {m}")));
            }
            notifyDictionary.Add("ELAPSED_TIME", GetElapsedTime(stopwatch));
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Intent Extractor Agent", notifyDictionary);
        }


        private async Task ExecuteQueryCacheServiceAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Queries Cache Service...");

            var notifyInputDictionary = new Dictionary<string, string>();
            if (state.MissingPastMemories.Any())
            {
                notifyInputDictionary.Add("MissingPastMemories", string.Join("\n", state.MissingPastMemories.Select(m => $"- {m}")));
            }
            if (state.MissingKnowledgeBaseSearchEntries.Any())
            {
                notifyInputDictionary.Add("MissingKnowledgeBaseEntries", string.Join("\n", state.MissingKnowledgeBaseSearchEntries.Select(m => $"- {m}")));
            }
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Queries Cache Service", notifyInputDictionary);

            var originalMemoryQueries = state.MissingPastMemories.ToList();
            var originalKnowledgeBaseQueries = state.MissingKnowledgeBaseSearchEntries.ToList();

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
                    state.ExtractedAgentMemories = cachedMemoryItemsList
                        .Select(item => new AgentMemoryQueryResultItem
                        {
                            Memory = item.Result,
                            Confidence = item.Relevance
                        })
                        .ToList();

                    var cachedQueries = cachedMemoryItemsList
                        .Select(item => item.SearchedQuery)
                        .Where(query => !string.IsNullOrWhiteSpace(query))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    state.MissingPastMemories = originalMemoryQueries.Where(q => !cachedQueries.Contains(q));
                }
            }

            if (originalKnowledgeBaseQueries.Any())
            {
                var knowledgeBaseQueries = originalKnowledgeBaseQueries
                    .Where(k => k.Type != KEYWORDS_SEARCH_TYPE) // ALWAYS Exclude keyword search queries from caching
                    .Select(entry => new KnowledgeBaseQueriesCacheItemInput
                    {
                        Query = entry.Query,
                        QueryType = ParseKnowledgeBaseSearchType(entry.Type)
                    })
                    .ToList();

                var cachedKnowledgeBaseResult = await _queriesCacheService.GetKnowledgeBaseCachedItemsAsync(knowledgeBaseQueries);
                totalTokensForEmbedding += cachedKnowledgeBaseResult.TotalTokens;

                var cachedKnowledgeBaseItemsList = cachedKnowledgeBaseResult.Items.ToList();

                if (cachedKnowledgeBaseItemsList.Any())
                {
                    var groupedCachedResults = cachedKnowledgeBaseItemsList
                        .GroupBy(item => new { item.SearchedQuery, item.SearchedQueryType })
                        .ToList();

                    state.KnowledgeBaseQueryResults = new KnowledgeBaseQueryResult
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
                            .ToList()
                    };

                    var cachedQueryKeys = groupedCachedResults
                        .Select(g => new { Query = g.Key.SearchedQuery, QueryType = g.Key.SearchedQueryType })
                        .ToHashSet();

                    state.MissingKnowledgeBaseSearchEntries = originalKnowledgeBaseQueries
                        .Where(entry => !cachedQueryKeys.Contains(new
                        {
                            Query = entry.Query,
                            QueryType = ParseKnowledgeBaseSearchType(entry.Type)
                        }));
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
                { "MemoryCacheHitsCount", (originalMemoryQueries.Count - state.MissingPastMemories.Count()).ToString() },
                { "MemoryRemainingQueriesCount", state.MissingPastMemories.Count().ToString() },
                { "KnowledgeBaseCacheHitsCount", (originalKnowledgeBaseQueries.Count - state.MissingKnowledgeBaseSearchEntries.Count()).ToString() },
                { "KnowledgeBaseRemainingQueriesCount", state.MissingKnowledgeBaseSearchEntries.Count().ToString() },
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
                { "MissingPastMemories", string.Join("\n", state.MissingPastMemories.Select(m => $"- {m}")) }
            });

            var queriesList = state.MissingPastMemories.ToList();

            var brcOutput = await _agentMemoryRetriever.ExecuteAsync(new AgentMemoryRetrieverInput
            {
                Query = string.Join(", ", queriesList)
            });

            var retrievedMemories = brcOutput.Items.ToList();
            state.ExtractedAgentMemories = state.ExtractedAgentMemories.Concat(retrievedMemories).ToList();

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
                { "ExtractedAgentMemories", string.Join("\n", retrievedMemories.Select(m => $"- {m.Memory}")) },
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
                { "MissingKnowledgeBaseEntries", string.Join("\n", state.MissingKnowledgeBaseSearchEntries.Select(m => $"- {m}")) }
            });

            var queriesList = state.MissingKnowledgeBaseSearchEntries.ToList();

            KnowledgeBaseQueryInput queryInput = new()
            {
                Collections = [DOCUMENTATION_COLLECTION_NAME],
                UserIntent = state.UserIntent,
                Queries = [.. queriesList.Select(entry => new KnowledgeBaseQueryInputItem
                {
                    Query = entry.Query,
                    SearchType = ParseKnowledgeBaseSearchType(entry.Type)
                })]
            };

            var brcOutput = await _knowledgeBaseSearchExecutor.ExecuteAsync(queryInput, CancellationToken.None);

            var existingResults = state.KnowledgeBaseQueryResults.Results.ToList();
            state.KnowledgeBaseQueryResults = new KnowledgeBaseQueryResult
            {
                Results = existingResults.Concat(brcOutput.Results).ToList()
            };

            if (_workflowConfiguration.EnableCacheService && brcOutput.Results.Any())
            {
                var cacheableQueries = queriesList
                    .Where(entry => ParseKnowledgeBaseSearchType(entry.Type) != KnowledgeBaseQuerySearchType.Keyword)
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
                                FoundQueryType = ParseKnowledgeBaseSearchType(query.Type),
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
                { "ExtractedKnowledgeBaseEntries", string.Join("\n", brcOutput.Results.Select(m => $"- File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")) },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("KB Search Service", notifyDictionary);
        }


        private async Task ExecuteContextAnalyzerAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Context Analyzer Agent...");
            var contextAnalyzerInputLogEntries = new Dictionary<string, string>
            {
                { "ExtranctedItent", state.UserIntent ?? "(no user intent)" }
            };
            if (state.ExtractedAgentMemories.Any())
            {
                contextAnalyzerInputLogEntries.Add("ExtractedAgentMemories", string.Join("\n", state.ExtractedAgentMemories.Select(m => $"- {m.Memory}")));
            }
            if (state.KnowledgeBaseQueryResults.Results.Any())
            {
                contextAnalyzerInputLogEntries.Add("ExtractedKnowledgeBaseDocuments", string.Join("\n", state.KnowledgeBaseQueryResults.Results.Select(m => $"- File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")));
            }
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Context Analyzer Agent", contextAnalyzerInputLogEntries);

            var sortedKnowledgeBaseResults = state.KnowledgeBaseQueryResults.Results
                .OrderByDescending(m => m.Relevance ?? double.MinValue)
                .ToList();

            var contextAnalyzerOutput = await _contextAnalyzerAgent.ExecuteAsync(new ContextAnalyzerAgentInput
            {
                UserIntent = state.UserIntent ?? string.Empty,
                ExtractedKnowledgeBase = [.. sortedKnowledgeBaseResults.Select(m => new ContextAnalyzerAgentInput.ExtractedKnowledgeItem
                {
                    DocumentId = m.Id,
                    Title = m.Title,
                    Summary = m.Summary
                })],
                ExtractedMemories = [.. state.ExtractedAgentMemories.Select(m => m.Memory)]
            });

            state.EnrichedUserRequest = contextAnalyzerOutput.CondensedUserIntent;
            state.UserIntentCategoryValue = contextAnalyzerOutput.UserIntentCategory;

            if (contextAnalyzerOutput.FilteredKnowledgeBaseDocuments != null
                && contextAnalyzerOutput.FilteredKnowledgeBaseDocuments.Any())
            {
                var filteredFileNames = state.KnowledgeBaseQueryResults.Results.Where(kb => contextAnalyzerOutput.FilteredKnowledgeBaseDocuments.Select(f => f.DocumentId).Contains(kb.Id))
                    .Select(kb => kb.File)
                    .Distinct()
                    .ToList();

                state.RelevantKnowledgeBaseFileNames = filteredFileNames;
            }
            state.AddTokenUsage(ContextAnalyzerAgentConfiguration.AgentName, contextAnalyzerOutput.InputTokenCount, contextAnalyzerOutput.OutputTokenCount, stopwatch.Elapsed, "Context Analyzer Agent");

            var contextAnalyzerOutputLogEntries = new Dictionary<string, string>
            {
                { "EnrichedUserRequest", state.EnrichedUserRequest },
                { "UserIntentCategory", state.UserIntentCategoryValue.ToString() }
            };

            if (state.RelevantKnowledgeBaseFileNames != null && state.RelevantKnowledgeBaseFileNames.Any())
            {
                contextAnalyzerOutputLogEntries.Add("KnowledgeBaseDocumentFilteredFiles", string.Join("\n", state.RelevantKnowledgeBaseFileNames.Select(m => $"- {m}")));
            }
            contextAnalyzerOutputLogEntries.Add("ELAPSED_TIME", GetElapsedTime(stopwatch));

            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Context Analyzer Agent", contextAnalyzerOutputLogEntries);
        }

        private async Task ExecuteTechnicalAnalystAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Technical Analyst Agent...");
            var enrichedUserRequest = state.EnrichedUserRequest ?? "(No enriched user request)";
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Technical Analyst Agent", new Dictionary<string, string>
            {
                { "EnrichedUserRequest", enrichedUserRequest },
                { "KnowledgeBaseDocumentsContent", state.KnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var serializedDocumentation = SerializeDocumentationForBusinessAnalyst(state.KnowledgeBaseDocumentsContent);

            var technicalAnalystOutput = await _technicalAnalystAgent.ExecuteAsync(new TechnicalAnalystAgentInput
            {
                EnrichedUserRequest = enrichedUserRequest,
                ApiDocumentation = serializedDocumentation
            }, cancellationToken);
            state.ShouldEngageCoder = true;
            state.BusinessRequirements = technicalAnalystOutput.BusinessRequirements;
            state.AddTokenUsage(TechnicalAnalystAgentConfiguration.AgentName, technicalAnalystOutput.InputTokenCount, technicalAnalystOutput.OutputTokenCount, stopwatch.Elapsed, "Technical Analyst Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "BusinessRequirements", technicalAnalystOutput.BusinessRequirements },
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
                { "BusinessRequirements", businessRequirements }
            });

            var serializedDocumentation = SerializeDocumentationForCoder(state.KnowledgeBaseDocumentsContent);

            var coderAgentOutput = await _coderAgent.ExecuteAsync(new CoderAgentInput
            {
                BusinessRequirements = businessRequirements,
                ApiDocumentation = serializedDocumentation
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


        private async Task ExecuteCodeStaticAnalyzerAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Code Static Analyzer Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Code Static Analyzer Agent", new Dictionary<string, string>
            {
                { "CodeToFix", state.LastCodeWithLineNumbers ?? "(No code available)" }
            });

            var staticAnalyzerOutput = await _codeStaticAnalyzer.ExecuteAsync(new CodeStaticAnalyzerInput
            {
                CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty
            });
            state.IsCodeValid = !staticAnalyzerOutput.Violations.Any();
            if (!state.IsCodeValid)
            {
                state.CodeIssues = [.. staticAnalyzerOutput.Violations];
            }
            else
            {
                state.CodeIssues.Clear();
            }
            state.AddTokenUsage(CodeStaticAnalyzerConfiguration.AgentName, staticAnalyzerOutput.InputTokenCount, staticAnalyzerOutput.OutputTokenCount, stopwatch.Elapsed, "Code Static Analyzer Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "IsCodeValid", state.IsCodeValid.ToString() },
                { "ViolationsCount", staticAnalyzerOutput.Violations.Count().ToString() },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Code Static Analyzer Agent", notifyDictionary);
        }

        private async Task ExecuteCodeFixerAsync(CodeModeWorkflowState state, int iteration, bool isRuntimeFix)
        {
            var stopwatch = Stopwatch.StartNew();
            var agentName = isRuntimeFix ? $"Code Fixer Agent for Runtime Errors (Iteration {iteration})" : $"Code Fixer Agent (Iteration {iteration})";

            _logger.LogDebug("Engaging Code Fixer Agent... Iteration {Iteration}", iteration);
            await _workflowProgressNotifier.NotifyWorkflowStepStart(agentName, new Dictionary<string, string>
            {
                { "CodeToFix", state.LastCodeWithLineNumbers ?? "(No code available)" },
                { "IssuesCount", state.CodeIssues.Count.ToString() }
            });

            var codeFixerOutput = await _codeFixerAgent.ExecuteAsync(new CodeFixerAgentInput
            {
                CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty,
                Issues = state.CodeIssues
            });
            state.GeneratedCode = codeFixerOutput.FixedCode;
            state.CodeFixerIterationCount++;
            state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount, stopwatch.Elapsed, agentName);
            var notifyDictionary = new Dictionary<string, string>
            {
                { "FixedCode", state.GeneratedCode },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd(agentName, notifyDictionary);
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

        private async Task ExecuteResultsPresenterAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Results Presenter Agent...");
            var sandboxResult = state.SandboxResult ?? "(No sandbox result)";
            var enrichedUserRequest = state.EnrichedUserRequest ?? "(No enriched user request)";
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


        private async Task ExecuteBusinessAdvisorAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Business Advisor Agent...");
            var enrichedUserRequest = state.EnrichedUserRequest ?? "(No enriched user request)";
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Business Advisor Agent", new Dictionary<string, string>
            {
                { "EnrichedUserRequest", enrichedUserRequest },
                { "KnowledgeBaseDocumentsContent", state.KnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var serializedDocumentation = SerializeDocumentationForBusinessAnalyst(state.KnowledgeBaseDocumentsContent);

            var baOutput = await _businessAdvisorAgent.ExecuteAsync(new BusinessAdvisorAgentInput
            {
                EnrichedUserRequest = enrichedUserRequest,
                Documentation = serializedDocumentation
            }, cancellationToken);
            state.BusinessAdvisorContent = baOutput.Content;
            state.AddTokenUsage(BusinessAdvisorAgentConfiguration.AgentName, baOutput.InputTokenCount, baOutput.OutputTokenCount, stopwatch.Elapsed, "Business Advisor Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "Content", state.BusinessAdvisorContent ?? "(No business advisor content)" },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Business Advisor Agent", notifyDictionary);
        }

        private async Task ExecuteDocumentationAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Documentation Agent...");
            var enrichedUserRequest = state.EnrichedUserRequest ?? "(No enriched user request)";
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Documentation Agent", new Dictionary<string, string>
            {
                { "EnrichedUserRequest", enrichedUserRequest },
                { "KnowledgeBaseDocumentsContent", state.KnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var serializedDocumentation = SerializeDocumentationForBusinessAnalyst(state.KnowledgeBaseDocumentsContent);

            var output = await _documentationAgent.ExecuteAsync(new DocumentationAgentInput
            {
                EnrichedUserRequest = enrichedUserRequest,
                Documentation = serializedDocumentation
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

        private static string SerializeDocumentationForBusinessAnalyst(IEnumerable<KnowledgeBaseDocumentContent> documents) => SerializeDocumentationFor(documents, DOCUMENTATION_FOR_BUSINESSANALYST_SECTIONTITLE);

        private static string SerializeDocumentationForCoder(IEnumerable<KnowledgeBaseDocumentContent> documents) => SerializeDocumentationFor(documents, DOCUMENTATION_FOR_DEVELOPER_SECTIONTITLE);

        private static string SerializeDocumentationFor(IEnumerable<KnowledgeBaseDocumentContent> documents, string separator)
        {
            var serializedDocs = documents.Select(kv => $"{MarkdownDocumentationHelper.GetMarkdownSection(kv.Content, separator)}\n\nOriginal file: {kv.File}");
            return string.Join(Environment.NewLine + "---" + Environment.NewLine + "---", serializedDocs);
        }

        private async Task CompleteWorkflowAsync(CodeModeWorkflowState state, string? data = null)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Personal Assistant Agent...");
            var enrichedUserRequest = state.EnrichedUserRequest ?? "(No enriched user request)";
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Personal Assistant Agent", new Dictionary<string, string>
            {
                { "Data", data ?? "(No data)" },
                { "EnrichedUserRequest", enrichedUserRequest },
                { "LanguageOfTheUser", state.LanguageOfTheUser ?? "(No language specified)" }
            });

            var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
            {
                Data = data,
                EnrichedUserRequest = enrichedUserRequest,
                LanguageOfTheUser = state.LanguageOfTheUser
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

        private async Task<bool> ExecuteRelevantFactsEvaluatorAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Relevant Facts Evaluator Agent...");
            var enrichedUserRequest = state.EnrichedUserRequest ?? "(No enriched user request)";
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Relevant Facts Evaluator Agent", new Dictionary<string, string>
            {
                { "User's request", enrichedUserRequest },
                { "AI pipeline answer", state.FinalAnswer ?? string.Empty }
            });

            var output = await _relevantFactsEvaluatorAgent.ExecuteAsync(new RelevantFactsEvaluatorAgentInput
            {
                EnrichedUserRequest = enrichedUserRequest,
                FinalAnswer = state.FinalAnswer ?? string.Empty
            });

            state.AddTokenUsage(RelevantFactsEvaluatorAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Relevant Facts Evaluator Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "IsWorthSaving", output.IsWorthSaving.ToString() },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Relevant Facts Evaluator Agent", notifyDictionary);

            return output.IsWorthSaving;
        }

        private static string GetElapsedTime(Stopwatch stopwatch) => $"{stopwatch.ElapsedMilliseconds}ms";

        private async Task ExecuteAgentMemorySaverAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Agent Memory Saver...");
            var enrichedUserRequest = state.EnrichedUserRequest ?? "(No enriched user request)";
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Agent Memory Saver", new Dictionary<string, string>
            {
                { "User's request", enrichedUserRequest },
                { "AI pipeline answer", state.FinalAnswer ?? string.Empty }
            });

            await _agentMemorySaver.ExecuteAsync(new AgentMemorySaverInput
            {
                MessageByUser = enrichedUserRequest,
                ResponseByAssistant = state.FinalAnswer ?? string.Empty
            });

            var notifyDictionary = new Dictionary<string, string>
            {
                { "Status", "Memory saved successfully" },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            state.AddStepUsage("Agent Memory Saver", stopwatch.Elapsed, false);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Agent Memory Saver", notifyDictionary);
        }


        private static string SearchTypeToString(KnowledgeBaseQuerySearchType searchType) => searchType switch
        {
            KnowledgeBaseQuerySearchType.Keyword => KEYWORDS_SEARCH_TYPE,
            KnowledgeBaseQuerySearchType.Semantic => SEMANTIC_SEARCH_TYPE,
            KnowledgeBaseQuerySearchType.HypotheticalDocument => HYPOTHETICAL_SEARCH_TYPE,
            _ => throw new ArgumentOutOfRangeException(nameof(searchType), $"Not expected search type value: {searchType}")
        };

        private static KnowledgeBaseQuerySearchType ParseKnowledgeBaseSearchType(string searchType) => searchType switch
        {
            KEYWORDS_SEARCH_TYPE => KnowledgeBaseQuerySearchType.Keyword,
            SEMANTIC_SEARCH_TYPE => KnowledgeBaseQuerySearchType.Semantic,
            HYPOTHETICAL_SEARCH_TYPE => KnowledgeBaseQuerySearchType.HypotheticalDocument,
            _ => throw new ArgumentOutOfRangeException(nameof(searchType), $"Not expected search type value: {searchType}")
        };

        public string GetIngressExecutorName() => IntentExtractorAgentConfiguration.AgentName;

        public string GetEgressExecutorName() => PersonalAssistantAgentConfiguration.AgentName;

        [System.Text.RegularExpressions.GeneratedRegex(@"\[\[(.*?)\]\]")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();
    }
}

