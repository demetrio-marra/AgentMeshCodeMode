using AgentMesh.Application.Models;
using AgentMesh.Services;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.KnowledgeBase;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Executors;

public class KnowledgeBaseServiceFastSearchWorkflowExecutor(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IKnowledgeBaseSearchFastExecutor knowledgeBaseSearchFastExecutor,
    IQueriesCacheService queriesCacheService,
    CodeModeWorkflowConfiguration workflowConfiguration)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IKnowledgeBaseSearchFastExecutor _knowledgeBaseSearchFastExecutor = knowledgeBaseSearchFastExecutor;
    private readonly IQueriesCacheService _queriesCacheService = queriesCacheService;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;

    public async Task ExecuteKnowledgeBaseServiceFastSearchAsync(
        CodeModeWorkflowState state,
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
        _logger.LogDebug(logMessage);

        await _workflowProgressNotifier.NotifyWorkflowStepStart(stepName, new Dictionary<string, string>
        {
            { startNotificationKey, getStartNotificationValue(state) }
        });

        var queries = buildQueries(state).ToList();

        if (!queries.Any())
        {
            _logger.LogDebug(noQueriesLogMessage);
            state.AddStepUsage(stepName, stopwatch.Elapsed, false);
            await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, new Dictionary<string, string>
            {
                { emptyResultNotificationKey, "(No queries generated)" },
                { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
            });
            return;
        }

        KnowledgeBaseQueryInput queryInput = new()
        {
            UserIntent = state.CanonicalizedIntent,
            Queries = queries,
            Collections = [collectionName]
        };

        var brcOutput = await _knowledgeBaseSearchFastExecutor.ExecuteAsync(queryInput, CancellationToken.None);

        setResults(state, new KnowledgeBaseQueryResult
        {
            Results = brcOutput.Results.ToList()
        });

        var cacheTokenUsageInfo = await KnowledgeBaseCacheUsageBuilder.BuildKnowledgeBaseCacheTokenUsageAsync(_workflowConfiguration.EnableCacheService, queries, brcOutput.Results, _queriesCacheService);
        state.AddStepUsage(stepName, stopwatch.Elapsed, cacheTokenUsageInfo is not null, cacheTokenUsageInfo);

        var notifyDictionary = new Dictionary<string, string>
        {
            { resultsNotificationKey, WorkflowExecutorFormatting.ToBulletList(brcOutput.Results.Select(m => $"File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")) },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
    }
}

