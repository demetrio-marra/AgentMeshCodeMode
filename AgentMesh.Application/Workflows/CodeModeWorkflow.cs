using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Models;
using AgentMesh.Models.ApiDocumentation;
using AgentMesh.Models.BusinessAdvisor;
using AgentMesh.Models.SemanticSearch;
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
using AgentMesh.Utilities;

namespace AgentMesh.Application.Workflows
{
    public class CodeModeWorkflow : IWorkflow
    {
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
        private readonly ISemanticSearchExecutor _semanticSearchExecutor;
        private readonly IApiDocumentationExecutor _apiDocumentationExecutor;
        private readonly IKnowledgeBaseService _knowledgeBaseService;

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
            ISemanticSearchExecutor semanticSearchExecutor,
            IApiDocumentationExecutor apiDocumentationExecutor,
            IKnowledgeBaseService knowledgeBaseService)
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
            _semanticSearchExecutor = semanticSearchExecutor;
            _apiDocumentationExecutor = apiDocumentationExecutor;
            _knowledgeBaseService = knowledgeBaseService;
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
            if (state.MissingKnowledgeBaseEntries.Any())
            {
                await ExecuteKnowledgeBaseServiceKeywordsSearchAsync(state);
            }

            await ExecuteContextAnalyzerAsync(state);

            if (state.UserIntentCategoryValue == UserIntentCategoryValues.Other)
            {
                goto CompleteWorkflow;
            }

            // FROM NOW ON, the workflow will be based on the user intent category (TaskExecution or Documentation)

            // if there are no knowledge base documents after filtering, search them again using semantic search
            if (!state.RelevantKnowledgeBaseFileNames.Any())
            {
                await ExecuteKnowledgeBaseServiceSemanticSearchAsync(state);    // overwrite the previous knowledge base query result with the new one
                await ExecuteContextAnalyzerAsync(state);

                if (state.UserIntentCategoryValue == UserIntentCategoryValues.Other)
                {
                    goto CompleteWorkflow;
                }
            }

            // now if we have knowledge base documents after filtering, we can fetch the whole documents content and store it in the state for later use
            if (state.RelevantKnowledgeBaseFileNames.Any())
            {
                // TODO: non si capisce perchè ma la multiget non funziona. Proviamo a usare la singola
                //var knowledgeBaseDocuments = await _knowledgeBaseService.GetKnowledgeBaseEntriesContentAsync(state.RelevantKnowledgeBaseFileNames, CancellationToken.None);

                var fetchedFilesContent = new Dictionary<string, string?>();
                foreach (var fileName in state.RelevantKnowledgeBaseFileNames)
                {
                    var document = await _knowledgeBaseService.GetKnowledgeBaseEntryContentAsync(fileName, CancellationToken.None);
                    if (document != null)
                    {
                        fetchedFilesContent.Add(fileName, document);
                    }
                }

                state.KnowledgeBaseDocumentsContent = state.KnowledgeBaseQueryResult
                    .Where(kb => fetchedFilesContent.ContainsKey(kb.File!))
                    .Select(kb => new KnowledgeBaseDocumentContent
                    {
                        Title = kb.Title,
                        File = kb.File,
                        Content = fetchedFilesContent[kb.File!] ?? string.Empty
                    }).ToList();
            }

            // temporary override just for tests
            state.UserIntentCategoryValue = UserIntentCategoryValues.Documentation;


            if (state.UserIntentCategoryValue == UserIntentCategoryValues.TaskExecution)
            {
                await ExecuteBusinessRequirementsCreatorAsync(state);
                await ExecuteApiDocumentationExecutorAsync(state);
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

            //await ExecuteAgentMemorySaverAsync(state);

            await _workflowProgressNotifier.NotifyWorkflowEnd();

            return new WorkflowResult
            {
                Response = state.FinalAnswer!,
                TokenUsageEntries = state.TokenUsageEntries
            };
        }

        private async Task ExecuteIntentExtractorAsync(CodeModeWorkflowState state, IEnumerable<ContextMessage> chatHistory)
        {
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
            state.MissingKnowledgeBaseEntries = intentExtractorOutput.MissingKnowledgeBaseEntries;

            state.AddTokenUsage(IntentExtractorAgentConfiguration.AgentName, intentExtractorOutput.TokenCount, intentExtractorOutput.InputTokenCount, intentExtractorOutput.OutputTokenCount);

            var notifyDictionary = new Dictionary<string, string>
            {
                { "ExtractedIntent", state.UserIntent ?? "(No intent extracted)" }
            };
            if (state.MissingPastMemories != null && state.MissingPastMemories.Any())
            {
                notifyDictionary.Add("MissingPastMemoriesDetails", string.Join("\n", state.MissingPastMemories.Select(m => $"- {m}")));
            }
            if (state.MissingKnowledgeBaseEntries != null && state.MissingKnowledgeBaseEntries.Any())
            {
                notifyDictionary.Add("MissingKnowledgeBaseEntriesDetails", string.Join("\n", state.MissingKnowledgeBaseEntries.Select(m => $"- {m}")));
            }
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Intent Extractor Agent", notifyDictionary);
        }

        private async Task ExecuteAgentMemoryServiceAsync(CodeModeWorkflowState state)
        {
            _logger.LogDebug("Engaging Agent Memory Service...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Agent Memory Service", new Dictionary<string, string>
            {
                { "MissingPastMemories", string.Join(", ", state.MissingPastMemories) }
            });

            var brcOutput = await _agentMemoryRetriever.ExecuteAsync(new AgentMemoryRetrieverInput
            {
                Query = string.Join(", ", state.MissingPastMemories)
            });

            state.ExtractedAgentMemories = brcOutput.Items.ToList();

            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Agent Memory Service", new Dictionary<string, string>
            {
                { "ExtractedAgentMemories", string.Join(", ", state.ExtractedAgentMemories.Select(m => m.Memory)) }
            });
        }

        private async Task ExecuteKnowledgeBaseServiceKeywordsSearchAsync(CodeModeWorkflowState state)
        {
            _logger.LogDebug("Engaging Knowledge Base Service (Keywords Search)...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Knowledge Base Service (Keywords Search)", new Dictionary<string, string>
            {
                { "MissingKnowledgeBaseEntries", string.Join(", ", state.MissingKnowledgeBaseEntries) }
            });

            var brcOutput = await _knowledgeBaseService.KeywordsSearch(state.MissingKnowledgeBaseEntries.ToList(), new[] { "apis-documentation" }, CancellationToken.None);

            state.KnowledgeBaseQueryResult = brcOutput.ToList();

            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Knowledge Base Service (Keywords Search)", new Dictionary<string, string>
            {
                { "ExtractedKnowledgeBaseEntries", string.Join(", ", state.KnowledgeBaseQueryResult.Select(m => $"ID: {m.Id}, Title: {m.Title}, Summary: {m.Summary}")) }
            });
        }

        private async Task ExecuteKnowledgeBaseServiceSemanticSearchAsync(CodeModeWorkflowState state)
        {
            _logger.LogDebug("Engaging Knowledge Base Service (Semantic Search)...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Knowledge Base Service (Semantic Search)", new Dictionary<string, string>
            {
                { "MissingKnowledgeBaseEntries", string.Join(", ", state.MissingKnowledgeBaseEntries) }
            });

            var brcOutput = await _knowledgeBaseService.SemanticSearchAsync(state.MissingKnowledgeBaseEntries.ToList(), new[] { "apis-documentation" }, rerank: true, CancellationToken.None);

            state.KnowledgeBaseQueryResult = brcOutput.ToList();

            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Knowledge Base Service (Semantic Search)", new Dictionary<string, string>
            {
                { "ExtractedKnowledgeBaseEntries", string.Join(", ", state.KnowledgeBaseQueryResult.Select(m => $"ID: {m.Id}, Title: {m.Title}, Summary: {m.Summary}")) }
            });
        }

        private async Task ExecuteContextAnalyzerAsync(CodeModeWorkflowState state)
        {
            _logger.LogDebug("Engaging Context Analyzer Agent...");
            var contextAnalyzerInputLogEntries = new Dictionary<string, string>
            {
                { "ExtranctedItent", state.UserIntent }
            };
            if (state.ExtractedAgentMemories.Any())
            {
                contextAnalyzerInputLogEntries.Add("ExtractedAgentMemories", string.Join(", ", state.ExtractedAgentMemories.Select(m => m.Memory)));
            }
            if (state.KnowledgeBaseQueryResult.Any())
            {
                contextAnalyzerInputLogEntries.Add("ExtractedKnowledgeBaseDocuments", string.Join(", ", state.KnowledgeBaseQueryResult.Select(m => $"File: {m.File}, Title: {m.Title}, Summary: {m.Summary}")));
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
            state.AddTokenUsage(ContextAnalyzerAgentConfiguration.AgentName, contextAnalyzerOutput.TokenCount, contextAnalyzerOutput.InputTokenCount, contextAnalyzerOutput.OutputTokenCount);

            var contextAnalyzerOutputLogEntries = new Dictionary<string, string>
            {
                { "EnrichedUserRequest", state.EnrichedUserRequest },
                { "UserIntentCategory", state.UserIntentCategoryValue.ToString() }
            };

            if (state.RelevantKnowledgeBaseFileNames != null && state.RelevantKnowledgeBaseFileNames.Any())
            {
                contextAnalyzerOutputLogEntries.Add("KnowledgeBaseDocumentFilteredIds", string.Join(", ", state.RelevantKnowledgeBaseFileNames));
            }

            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Context Analyzer Agent", contextAnalyzerOutputLogEntries);
        }

        private async Task ExecuteBusinessRequirementsCreatorAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            await ExecuteSemanticSearchAsync(state, BusinessRequirementsCreatorAgentConfiguration.AgentName, cancellationToken);

            _logger.LogDebug("Engaging Business Requirements Creator Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Business Requirements Creator Agent", new Dictionary<string, string>
            {
                { "EnrichedUserRequest", state.EnrichedUserRequest }
            });

            var brcOutput = await _businessRequirementsCreatorAgent.ExecuteAsync(new BusinessRequirementsCreatorAgentInput
            {
                EnrichedUserRequest = state.EnrichedUserRequest,
                ApiDocumentation = state.SemanticSearchApiDocumentation
            }, cancellationToken);
            state.ShouldEngageCoder = true;
            state.BusinessRequirements = brcOutput.BusinessRequirements;
            state.MentionedApis = brcOutput.MentionedApis;
            state.AddTokenUsage(BusinessRequirementsCreatorAgentConfiguration.AgentName, brcOutput.TokenCount, brcOutput.InputTokenCount, brcOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Business Requirements Creator Agent", new Dictionary<string, string>
            {
                { "BusinessRequirements", brcOutput.BusinessRequirements },
                { "MentionedApis", string.Join(", ", brcOutput.MentionedApis) }
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
                BusinessRequirements = state.BusinessRequirements!,
                ApiDocumentation = state.ApiDocumentation
            });
            state.GeneratedCode = coderAgentOutput.CodeToRun;
            state.AddTokenUsage(CoderAgentConfiguration.AgentName, coderAgentOutput.TokenCount, coderAgentOutput.InputTokenCount, coderAgentOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Coder Agent", new Dictionary<string, string>
            {
                { "CodeToRun", state.GeneratedCode }
            });
        }

        private async Task ExecuteApiDocumentationExecutorAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Engaging API Documentation Executor...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("API Documentation Executor", new Dictionary<string, string>
            {
                { "MentionedApis", string.Join(", ", state.MentionedApis) }
            });

            var apiDocOutput = await _apiDocumentationExecutor.ExecuteAsync(new ApiDocumentationExecutorInput
            {
                MentionedApis = state.MentionedApis
            }, cancellationToken);

            state.ApiDocumentation = apiDocOutput.ApiDocumentation;

            await _workflowProgressNotifier.NotifyWorkflowStepEnd("API Documentation Executor", new Dictionary<string, string>
            {
                { "ApiDocumentationLength", state.ApiDocumentation.Length.ToString() }
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

                var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
                {
                    Code = state.GeneratedCode
                });
                state.SandboxResult = executionOutput.Result;
                state.CodeExecutionResultType = SandboxResultType.Success;
                await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, new Dictionary<string, string>
                {
                    { "Result", state.SandboxResult }
                });
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
                await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, new Dictionary<string, string>
                {
                    { "Error", state.SandboxResult }
                });
            }
            catch (Exception ex)
            {
                state.SandboxResult = ex.Message;
                state.CodeExecutionResultType = SandboxResultType.ApplicationError;
                sandBoxError = true;
                await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, new Dictionary<string, string>
                {
                    { "Error", state.SandboxResult }
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
                { "Data", state.SandboxResult! },
                { "EnrichedUserRequest", state.EnrichedUserRequest }
            });

            var resultsPresenterOutput = await _resultsPresenterAgent.ExecuteAsync(new ResultsPresenterAgentInput
            {
                Data = state.SandboxResult!,
                EnrichedUserRequest = state.EnrichedUserRequest
            });
            state.PresenterOutput = resultsPresenterOutput.Content;
            state.AddTokenUsage(ResultsPresenterAgentConfiguration.AgentName, resultsPresenterOutput.TokenCount, resultsPresenterOutput.InputTokenCount, resultsPresenterOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Results Presenter Agent", new Dictionary<string, string>
            {
                { "Content", state.PresenterOutput }
            });
        }

        private async Task ExecuteSemanticSearchAsync(CodeModeWorkflowState state, string agentRole, CancellationToken cancellationToken = default)
        {
            //_logger.LogDebug("Engaging Semantic Search Executor...");
            //await _workflowProgressNotifier.NotifyWorkflowStepStart("Semantic Search Executor", new Dictionary<string, string>
            //{
            //    { "ActionableRequirements", string.Join(", ", state.ActionableRequirements) },
            //    { "AgentRole", agentRole }
            //});

            //var searchOutput = await _semanticSearchExecutor.ExecuteAsync(new SemanticSearchExecutorInput
            //{
            //    ActionableRequirements = state.ActionableRequirements,
            //    AgentRole = agentRole
            //}, cancellationToken);

            //state.SemanticSearchApiDocumentation = searchOutput.ApiDocumentation;

            //await _workflowProgressNotifier.NotifyWorkflowStepEnd("Semantic Search Executor", new Dictionary<string, string>
            //{
            //    { "ApiDocumentationLength", state.SemanticSearchApiDocumentation.Length.ToString() }
            //});
        }

        private async Task ExecuteBusinessAdvisorAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Engaging Business Advisor Agent...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Business Advisor Agent", new Dictionary<string, string>
            {
                { "EnrichedUserRequest", state.EnrichedUserRequest },
                { "KnowledgeBaseDocumentsContent", state.KnowledgeBaseDocumentsContent.Count().ToString() }
            });

            var baOutput = await _businessAdvisorAgent.ExecuteAsync(new BusinessAdvisorAgentInput
            {
                EnrichedUserRequest = state.EnrichedUserRequest,
                Documentation = string.Join(Environment.NewLine, state.KnowledgeBaseDocumentsContent.Select(kv => $"# {kv.Title}\n\n## File\n{kv.File}\n\n## Content:\n{kv.Content}\n"))
            }, cancellationToken);
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
                { "EnrichedUserRequest", state.EnrichedUserRequest }
            });

            var personalAssistantOutput = await _personalAssistantAgent.ExecuteAsync(new PersonalAssistantAgentInput
            {
                Data = data,
                EnrichedUserRequest = state.EnrichedUserRequest
            });
            state.FinalAnswer = personalAssistantOutput.Response;
            state.AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Personal Assistant Agent", new Dictionary<string, string>
            {
                { "Response", state.FinalAnswer }
            });
        }

        private async Task ExecuteAgentMemorySaverAsync(CodeModeWorkflowState state)
        {
            _logger.LogDebug("Engaging Agent Memory Saver...");
            await _workflowProgressNotifier.NotifyWorkflowStepStart("Agent Memory Saver", new Dictionary<string, string>
            {
                { "MessageByUser", state.OriginalUserRequest },
                { "ResponseByAssistant", state.FinalAnswer ?? string.Empty }
            });

            await _agentMemorySaver.ExecuteAsync(new AgentMemorySaverInput
            {
                MessageByUser = state.OriginalUserRequest,
                ResponseByAssistant = state.FinalAnswer ?? string.Empty
            });

            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Agent Memory Saver", new Dictionary<string, string>
            {
                { "Status", "Memory saved successfully" }
            });
        }


        public string GetIngressExecutorName() => IntentExtractorAgentConfiguration.AgentName;

        public string GetEgressExecutorName() => PersonalAssistantAgentConfiguration.AgentName;
    }
}
