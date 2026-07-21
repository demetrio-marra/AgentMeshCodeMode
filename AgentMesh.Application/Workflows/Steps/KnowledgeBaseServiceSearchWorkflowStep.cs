using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class KnowledgeBaseServiceSearchWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    KnowledgeBaseExecutor knowledgeBaseSearchExecutor) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Knowledge Base Service Search";

    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly KnowledgeBaseExecutor _knowledgeBaseSearchExecutor = knowledgeBaseSearchExecutor;

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
            UserIntent = state.Intent,
            Queries = queriesList
        };

        var brcOutput = await _knowledgeBaseSearchExecutor.QueryAsync(queryInput, CancellationToken.None);

        var existingResults = getExistingResults(state).Results.ToList();
        setResults(state, new KnowledgeBaseQueryResult
        {
            Results = existingResults.Concat(brcOutput.Results).ToList()
        });

        var notifyDictionary = new Dictionary<string, string>
        {
            { "ExtractedKnowledgeBaseEntries", WorkflowExecutorFormatting.ToBulletList(brcOutput.Results.Select(m => $"File: {m.File}, Title: {m.Title}, Relevance: {m.Relevance}")) },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        await ExecuteKnowledgeBaseServiceSearchAsync(
            stateObject,
            "KB Search Service",
            "domains",
            workflowState => workflowState.DomainsKnowledgeBaseQuery,
            workflowState => workflowState.DomainsKnowledgeBaseQueryResults,
            (workflowState, queryResult) => workflowState.DomainsKnowledgeBaseQueryResults = queryResult);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}
