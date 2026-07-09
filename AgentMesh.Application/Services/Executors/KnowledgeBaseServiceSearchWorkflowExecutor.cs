using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Executors;

public class KnowledgeBaseServiceSearchWorkflowExecutor(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IKnowledgeBaseSearchExecutor knowledgeBaseSearchExecutor,
    IQueriesCacheService queriesCacheService,
    CodeModeWorkflowConfiguration workflowConfiguration)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IKnowledgeBaseSearchExecutor _knowledgeBaseSearchExecutor = knowledgeBaseSearchExecutor;
    private readonly IQueriesCacheService _queriesCacheService = queriesCacheService;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;

    public async Task ExecuteKnowledgeBaseServiceSearchAsync(
        CodeModeWorkflowState state,
        string stepName,
        string collectionName,
        Func<CodeModeWorkflowState, IEnumerable<KnowledgeBaseQueryInputItem>> getQueries,
        Func<CodeModeWorkflowState, KnowledgeBaseQueryResult> getExistingResults,
        Action<CodeModeWorkflowState, KnowledgeBaseQueryResult> setResults)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Knowledge Base Service...");
        await _workflowProgressNotifier.NotifyWorkflowStepStart(stepName, new Dictionary<string, string>
        {
            { "MissingKnowledgeBaseEntries", WorkflowExecutorFormatting.ToBulletList(getQueries(state)) }
        });

        var queriesList = getQueries(state).ToList();

        KnowledgeBaseQueryInput queryInput = new()
        {
            Collections = [collectionName],
            UserIntent = state.CanonicalizedIntent,
            Queries = queriesList
        };

        var brcOutput = await _knowledgeBaseSearchExecutor.ExecuteAsync(queryInput, CancellationToken.None);

        var existingResults = getExistingResults(state).Results.ToList();
        setResults(state, new KnowledgeBaseQueryResult
        {
            Results = existingResults.Concat(brcOutput.Results).ToList()
        });

        var cacheTokenUsageInfo = await KnowledgeBaseCacheUsageBuilder.BuildKnowledgeBaseCacheTokenUsageAsync(_workflowConfiguration.EnableCacheService, queriesList, brcOutput.Results, _queriesCacheService);
        state.AddStepUsage(stepName, stopwatch.Elapsed, cacheTokenUsageInfo is not null, cacheTokenUsageInfo);

        var notifyDictionary = new Dictionary<string, string>
        {
            { "ExtractedKnowledgeBaseEntries", WorkflowExecutorFormatting.ToBulletList(brcOutput.Results.Select(m => $"File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")) },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
    }
}
