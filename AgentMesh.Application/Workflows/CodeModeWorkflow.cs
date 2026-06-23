using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Models;
using AgentMesh.Models.BusinessAdvisor;
using AgentMesh.Models.BusinessRequirementsCreator;
using AgentMesh.Models.CodeExecutionFailuresDetector;
using AgentMesh.Models.CodeFixer;
using AgentMesh.Models.Coder;
using AgentMesh.Models.CodeSandbox;
using AgentMesh.Models.CodeStaticAnalyzer;
using AgentMesh.Models.ContextAnalyzer;
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

namespace AgentMesh.Application.Workflows
{
    public class CodeModeWorkflow : IWorkflow
    {
        private const string DOCUMENTATION_FOR_BUSINESSANALYST_SECTIONTITLE = "Documentation";
        private const string DOCUMENTATION_FOR_DEVELOPER_SECTIONTITLE = "Technical reference";
        private const string DOCUMENTATION_COLLECTION_NAME = "documentation";
        private const bool AUTOMATICALLY_FETCH_RELATED_DOCUMENTATION = true;

        private readonly ILogger<CodeModeWorkflow> _logger;
        private readonly IWorkflowProgressNotifier _workflowProgressNotifier;

        private readonly IBusinessRequirementsCreatorAgent _businessRequirementsCreatorAgent;
        private readonly IBusinessAdvisorAgent _businessAdvisorAgent;
        private readonly ICoderAgent _coderAgent;
        private readonly ICodeStaticAnalyzerAgent _codeStaticAnalyzer;
        private readonly ICodeFixerAgent _codeFixerAgent;
        private readonly ICodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent;
        private readonly IResultsPresenterAgent _resultsPresenterAgent;
        private readonly IJSSandboxExecutor _jsSandboxExecutor;
        private readonly IIntentExtractorAgent _intentExtractorAgent;
        private readonly IPersonalAssistantAgent _personalAssistantAgent;
        private readonly IContextAnalyzerAgent _contextAnalyzerAgent;
        private readonly IAgentMemoryRetriever _agentMemoryRetriever;
        private readonly IAgentMemorySaver _agentMemorySaver;
        private readonly IKnowledgeBaseSearchExecutor _knowledgeBaseSearchExecutor;
        private readonly IKnowledgeBaseGetDocsExecutor _knowledgeBaseGetDocsExecutor;
        private readonly IRelevantFactsEvaluatorAgent _relevantFactsEvaluatorAgent;

        public CodeModeWorkflow(ILogger<CodeModeWorkflow> logger,
            IWorkflowProgressNotifier workflowProgressNotifier,
            IBusinessRequirementsCreatorAgent businessRequirementsCreatorAgent,
            IBusinessAdvisorAgent businessAdvisorAgent,
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
            IRelevantFactsEvaluatorAgent relevantFactsEvaluatorAgent) 
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
            _intentExtractorAgent = intentExtractorAgent;
            _personalAssistantAgent = personalAssistantAgent;
            _contextAnalyzerAgent = contextAnalyzerAgent;
            _agentMemoryRetriever = agentMemoryRetriever;
            _agentMemorySaver = agentMemorySaver;
            _knowledgeBaseSearchExecutor = knowledgeBaseSearchExecutor;
            _knowledgeBaseGetDocsExecutor = knowledgeBaseGetDocsExecutor;
            _relevantFactsEvaluatorAgent = relevantFactsEvaluatorAgent;
        }

        public async Task<WorkflowResult> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory)
        {
            await _workflowProgressNotifier.NotifyWorkflowStart();

            var state = new CodeModeWorkflowState(userInput, chatHistory);

            _logger.LogDebug("Extracting user intent...");

            await ExecuteIntentExtractorAsync(state, chatHistory);
            if (state.MissingPastMemories.Any())
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

            // now if we have knowledge base documents after filtering, we can fetch the whole documents content and store it in the state for later use
            if (state.RelevantKnowledgeBaseFileNames.Any())
            {             
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
                        var matches = System.Text.RegularExpressions.Regex.Matches(mainDoc.Content, @"\[\[(.*?)\]\]");
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
                    relatedDocs = relatedDocs.Distinct().ToList();

                    // avoid extracting already fetched docs
                    relatedDocs = relatedDocs.Except(fetchedFilesContent.Results.Select(r => r.File)).ToList();

                    // fetch again the content of the related docs and add them to the fetchedFilesContent.Results
                    if (relatedDocs.Any())
                    {
                        var relatedDocsContent = await _knowledgeBaseGetDocsExecutor.ExecuteAsync(new AgentMesh.Models.KnowledgeBase.KnowledgeBaseGetDocsInput
                        {
                            FilePaths = relatedDocs
                        });
                        fetchedFilesContent.Results = fetchedFilesContent.Results.Concat(relatedDocsContent.Results);
                    }
                }

                state.KnowledgeBaseDocumentsContent = state.KnowledgeBaseQueryResult
                    .Join(fetchedFilesContent.Results, kb => kb.File, fc => fc.File, (kb, fc) => new { kb, fc })
                    .Select(kb => new KnowledgeBaseDocumentContent
                    {
                        File = kb.kb.File,
                        Content = kb.fc.Content
                    }).ToList();
            }

            if (state.UserIntentCategoryValue == UserIntentCategoryValues.TaskExecution)
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

                        sandBoxError = await ExecuteJSSandboxAsync(state, true);
                        if (sandBoxError)
                        {
                            break;
                        }
                    }

                    await ExecuteResultsPresenterAsync(state, sandBoxError);
                    await CompleteWorkflowAsync(state, state.PresenterOutput);
                }
                else
                {
                    await ExecuteResultsPresenterAsync(state, sandBoxError);
                    await CompleteWorkflowAsync(state, state.PresenterOutput);
                }
                goto WorkflowEnd;
            }
            else if (state.UserIntentCategoryValue == UserIntentCategoryValues.Documentation)
            {
                await ExecuteBusinessAdvisorAsync(state);
                await CompleteWorkflowAsync(state, state.BusinessAdvisorContent);
                goto WorkflowEnd;
            }
            else
            {
                throw new Exception($"Unknown user intent category: {state.UserIntentCategoryValue}");
            }

        CompleteWorkflow:
            await CompleteWorkflowAsync(state);

        WorkflowEnd:

            var isWorthSaving = await ExecuteRelevantFactsEvaluatorAsync(state);
            if (isWorthSaving)
            {
                await ExecuteAgentMemorySaverAsync(state);
            }

            //await ExecuteAgentMemorySaverAsync(state);

            await _workflowProgressNotifier.NotifyWorkflowEnd();

            return new WorkflowResult
            {
                Response = state.FinalAnswer!,
                UsageStatistics = state.TokenUsageEntries
            };
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
                ContextMessages = state.InitialContextMessages.ToList(),
                UserLastRequest = state.OriginalUserRequest
            });
            
            state.UserIntent = intentExtractorOutput.UserIntent;
            state.MissingPastMemories = intentExtractorOutput.MissingPastMemories;
            state.MissingKnowledgeBaseSearchEntries = intentExtractorOutput.MissingKnowledgeBaseSearchEntries;

            state.AddTokenUsage(IntentExtractorAgentConfiguration.AgentName, intentExtractorOutput.TokenCount, intentExtractorOutput.InputTokenCount, intentExtractorOutput.OutputTokenCount, stopwatch.Elapsed, "Intent Extractor Agent");

            var notifyDictionary = new Dictionary<string, string>
            {
                { "ExtractedIntent", state.UserIntent ?? "(No intent extracted)" }
            };
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


        private async Task ExecuteAgentMemoryServiceAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Agent Memory Service...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Agent Memory Service", new Dictionary<string, string>
            {
                { "MissingPastMemories", string.Join("\n", state.MissingPastMemories.Select(m => $"- {m}")) }
            });

            var brcOutput = await _agentMemoryRetriever.ExecuteAsync(new AgentMemoryRetrieverInput
            {
                Query = string.Join(", ", state.MissingPastMemories)
            });

            state.ExtractedAgentMemories = brcOutput.Items.ToList();
            state.AddStepUsage("Agent Memory Service", stopwatch.Elapsed, false);

            var notifyDictionary = new Dictionary<string, string>
            {
                { "ExtractedAgentMemories", string.Join("\n", state.ExtractedAgentMemories.Select(m => $"- {m.Memory}")) },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Agent Memory Service", notifyDictionary);
        }


        private async Task ExecuteKnowledgeBaseServiceSearchAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Knowledge Base Service (Full Search)...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Knowledge Base Service (Full Search)", new Dictionary<string, string>
            {
                { "MissingKnowledgeBaseEntries", string.Join("\n", state.MissingKnowledgeBaseSearchEntries.Select(m => $"- {m}")) }
            });

            KnowledgeBaseQueryInput queryInput = new KnowledgeBaseQueryInput
            {
                Collections = new[] { DOCUMENTATION_COLLECTION_NAME },
                UserIntent = state.UserIntent,
                Queries = state.MissingKnowledgeBaseSearchEntries.Select(entry => new KnowledgeBaseQueryInputItem
                {
                    Query = entry.Query,
                    SearchType = entry.Query == "lex" ? AgentMesh.Models.KnowledgeBase.KnowledgeBaseQuerySearchType.Keyword : entry.Query == "vec" ? AgentMesh.Models.KnowledgeBase.KnowledgeBaseQuerySearchType.Semantic : AgentMesh.Models.KnowledgeBase.KnowledgeBaseQuerySearchType.HypotethicalDocument
                }).ToList()
            };

            var brcOutput = await _knowledgeBaseSearchExecutor.ExecuteAsync(queryInput, CancellationToken.None);

            state.KnowledgeBaseQueryResult = brcOutput.Results.Select(r => new Models.KnowledgeBaseQueryResult
            {
                Id = r.Id,
                File = r.File,
                Title = r.Title,
                Summary = r.Summary,
                Relevance = r.Relevance
            }).ToList();
            state.AddStepUsage("Knowledge Base Service (Full Search)", stopwatch.Elapsed, false);

            var notifyDictionary = new Dictionary<string, string>
            {
                { "ExtractedKnowledgeBaseEntries", string.Join("\n", state.KnowledgeBaseQueryResult.Select(m => $"- File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")) },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Knowledge Base Service (Full Search)", notifyDictionary);
        }


        private async Task ExecuteContextAnalyzerAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Context Analyzer Agent...");
            var contextAnalyzerInputLogEntries = new Dictionary<string, string>
            {
                { "ExtranctedItent", state.UserIntent }
            };
            if (state.ExtractedAgentMemories.Any())
            {
                contextAnalyzerInputLogEntries.Add("ExtractedAgentMemories", string.Join("\n", state.ExtractedAgentMemories.Select(m => $"- {m.Memory}")));
            }
            if (state.KnowledgeBaseQueryResult.Any())
            {
                contextAnalyzerInputLogEntries.Add("ExtractedKnowledgeBaseDocuments", string.Join("\n", state.KnowledgeBaseQueryResult.Select(m => $"- File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")));
            }
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Context Analyzer Agent", contextAnalyzerInputLogEntries);

            var contextAnalyzerOutput = await _contextAnalyzerAgent.ExecuteAsync(new ContextAnalyzerAgentInput
            {
                UserIntent = state.UserIntent ?? string.Empty,
                ExtractedKnowledgeBase = state.KnowledgeBaseQueryResult.Select(m => new ContextAnalyzerAgentInput.ExtractedKnowledgeItem
                {
                    DocumentId = m.Id,
                    Title = m.Title,
                    Summary = m.Summary,
                    Relevance = m.Relevance                     
                }).ToList(),
                ExtractedMemories = state.ExtractedAgentMemories.Select(m => m.Memory).ToList()
            });
            
            state.EnrichedUserRequest = contextAnalyzerOutput.CondensedUserIntent;
            state.UserIntentCategoryValue = contextAnalyzerOutput.UserIntentCategory;

            if (contextAnalyzerOutput.FilteredKnowledgeBaseDocuments != null
                && contextAnalyzerOutput.FilteredKnowledgeBaseDocuments.Any())
            {
                var filteredFileNames = state.KnowledgeBaseQueryResult.Where(kb => contextAnalyzerOutput.FilteredKnowledgeBaseDocuments.Select(f => f.DocumentId).Contains(kb.Id))
                    .Select(kb => kb.File)
                    .Distinct()
                    .ToList();

                state.RelevantKnowledgeBaseFileNames = filteredFileNames;
            }
            state.AddTokenUsage(ContextAnalyzerAgentConfiguration.AgentName, contextAnalyzerOutput.TokenCount, contextAnalyzerOutput.InputTokenCount, contextAnalyzerOutput.OutputTokenCount, stopwatch.Elapsed, "Context Analyzer Agent");

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

        private async Task ExecuteBusinessRequirementsCreatorAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Business Requirements Creator Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Business Requirements Creator Agent", new Dictionary<string, string>
            {
                { "EnrichedUserRequest", state.EnrichedUserRequest },
                { "KnowledgeBaseDocumentsContent", state.KnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var serializedDocumentation = SerializeDocumentationForBusinessAnalyst(state.KnowledgeBaseDocumentsContent);

            var brcOutput = await _businessRequirementsCreatorAgent.ExecuteAsync(new BusinessRequirementsCreatorAgentInput
            {
                EnrichedUserRequest = state.EnrichedUserRequest,
                ApiDocumentation = serializedDocumentation
            }, cancellationToken);
            state.ShouldEngageCoder = true;
            state.BusinessRequirements = brcOutput.BusinessRequirements;
            state.AddTokenUsage(BusinessRequirementsCreatorAgentConfiguration.AgentName, brcOutput.TokenCount, brcOutput.InputTokenCount, brcOutput.OutputTokenCount, stopwatch.Elapsed, "Business Requirements Creator Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "BusinessRequirements", brcOutput.BusinessRequirements },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Business Requirements Creator Agent", notifyDictionary);
        }

        private async Task ExecuteCoderAsync(CodeModeWorkflowState state)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Coder Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Coder Agent", new Dictionary<string, string>
            {
                { "BusinessRequirements", state.BusinessRequirements! }
            });

            var serializedDocumentation = SerializeDocumentationForCoder(state.KnowledgeBaseDocumentsContent);

            var coderAgentOutput = await _coderAgent.ExecuteAsync(new CoderAgentInput
            {
                BusinessRequirements = state.BusinessRequirements!,
                ApiDocumentation = serializedDocumentation
            });
            state.GeneratedCode = coderAgentOutput.CodeToRun;
            state.AddTokenUsage(CoderAgentConfiguration.AgentName, coderAgentOutput.TokenCount, coderAgentOutput.InputTokenCount, coderAgentOutput.OutputTokenCount, stopwatch.Elapsed, "Coder Agent");
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
            state.AddTokenUsage(CodeStaticAnalyzerConfiguration.AgentName, staticAnalyzerOutput.TokenCount, staticAnalyzerOutput.InputTokenCount, staticAnalyzerOutput.OutputTokenCount, stopwatch.Elapsed, "Code Static Analyzer Agent");
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
            state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.TokenCount, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount, stopwatch.Elapsed, agentName);
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
            var logMessage = isReexecution ? "Re-executing JS Sandbox Executor after runtime fix..." : "Engaging JS Sandbox Executor...";

            bool sandBoxError = false;
            try
            {
                _logger.LogDebug(logMessage);
                await _workflowProgressNotifier.NotifyWorkflowStepStart(stepName, new Dictionary<string, string>
                {
                    { "Code", state.GeneratedCode }
                });

                var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
                {
                    Code = state.GeneratedCode
                });
                state.SandboxResult = executionOutput.Result;
                state.CodeExecutionResultType = SandboxResultType.Success;
                var notifyDictionary = new Dictionary<string, string>
                {
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
                { "CodeWithLineNumbers", state.LastCodeWithLineNumbers },
                { "ExecutionResult", state.SandboxResult! }
            });

            var detectorOutput = await _codeExecutionFailuresDetectorAgent.ExecuteAsync(new CodeExecutionFailuresDetectorAgentInput
            {
                CodeWithLineNumbers = state.LastCodeWithLineNumbers,
                ExecutionResult = state.SandboxResult!
            });
            state.CodeExecutionFailuresDetectorIterationCount++;
            state.AddTokenUsage(CodeExecutionFailuresDetectorAgentConfiguration.AgentName, detectorOutput.TokenCount, detectorOutput.InputTokenCount, detectorOutput.OutputTokenCount, stopwatch.Elapsed, $"Code Execution Failures Detector Agent (Iteration {iteration})");
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
                { "CodeToFix", state.LastCodeWithLineNumbers },
                { "IssuesCount", "1" }
            });

            var codeFixerOutput = await _codeFixerAgent.ExecuteAsync(new CodeFixerAgentInput
            {
                CodeToFix = state.LastCodeWithLineNumbers,
                Issues = new[] { analysis }
            });
            state.GeneratedCode = codeFixerOutput.FixedCode;
            state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.TokenCount, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount, stopwatch.Elapsed, $"Code Fixer Agent for Runtime Errors (Iteration {iteration})");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "FixedCode", state.GeneratedCode },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd($"Code Fixer Agent for Runtime Errors (Iteration {iteration})", notifyDictionary);
        }

        private async Task ExecuteResultsPresenterAsync(CodeModeWorkflowState state, bool sandBoxError)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Engaging Results Presenter Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Results Presenter Agent", new Dictionary<string, string>
            {
                { "Data", state.SandboxResult! },
                { "EnrichedUserRequest", state.EnrichedUserRequest }
            });

            var resultsPresenterOutput = await _resultsPresenterAgent.ExecuteAsync(new ResultsPresenterAgentInput
            {
                Data = state.SandboxResult!,
                EnrichedUserRequest = state.EnrichedUserRequest
            });
            state.PresenterOutput = resultsPresenterOutput.Content;
            state.AddTokenUsage(ResultsPresenterAgentConfiguration.AgentName, resultsPresenterOutput.TokenCount, resultsPresenterOutput.InputTokenCount, resultsPresenterOutput.OutputTokenCount, stopwatch.Elapsed, "Results Presenter Agent");
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
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Business Advisor Agent", new Dictionary<string, string>
            {
                { "EnrichedUserRequest", state.EnrichedUserRequest },
                { "KnowledgeBaseDocumentsContent", state.KnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var serializedDocumentation = SerializeDocumentationForBusinessAnalyst(state.KnowledgeBaseDocumentsContent);

            var baOutput = await _businessAdvisorAgent.ExecuteAsync(new BusinessAdvisorAgentInput
            {
                EnrichedUserRequest = state.EnrichedUserRequest,
                Documentation = serializedDocumentation
            }, cancellationToken);
            state.BusinessAdvisorContent = baOutput.Content;
            state.AddTokenUsage(BusinessAdvisorAgentConfiguration.AgentName, baOutput.TokenCount, baOutput.InputTokenCount, baOutput.OutputTokenCount, stopwatch.Elapsed, "Business Advisor Agent");
            var notifyDictionary = new Dictionary<string, string>
            {
                { "Content", state.BusinessAdvisorContent },
                { "ELAPSED_TIME", GetElapsedTime(stopwatch) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Business Advisor Agent", notifyDictionary);
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
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Personal Assistant Agent", new Dictionary<string, string>
            {
                { "Data", data ?? "(No data)" },
                { "EnrichedUserRequest", state.EnrichedUserRequest }
            });

            var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
            {
                Data = data,
                EnrichedUserRequest = state.EnrichedUserRequest
            });
            state.FinalAnswer = personalAssistantOutput.Response;
            state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount, stopwatch.Elapsed, "Personal Assistant Agent");
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
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Relevant Facts Evaluator Agent", new Dictionary<string, string>
            {
                { "User's request", state.EnrichedUserRequest },
                { "AI pipeline answer", state.FinalAnswer ?? string.Empty }
            });

            var output = await _relevantFactsEvaluatorAgent.ExecuteAsync(new RelevantFactsEvaluatorAgentInput
            {
                EnrichedUserRequest = state.EnrichedUserRequest,
                FinalAnswer = state.FinalAnswer ?? string.Empty
            });

            state.AddTokenUsage(RelevantFactsEvaluatorAgentConfiguration.AgentName, output.TokenCount, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Relevant Facts Evaluator Agent");
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
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Agent Memory Saver", new Dictionary<string, string>
            {
                { "User's request", state.EnrichedUserRequest },
                { "AI pipeline answer", state.FinalAnswer ?? string.Empty }
            });

            await _agentMemorySaver.ExecuteAsync(new AgentMemorySaverInput
            {
                MessageByUser = state.EnrichedUserRequest,
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


        public string GetIngressExecutorName() => IntentExtractorAgentConfiguration.AgentName;

        public string GetEgressExecutorName() => PersonalAssistantAgentConfiguration.AgentName;
    }
}
