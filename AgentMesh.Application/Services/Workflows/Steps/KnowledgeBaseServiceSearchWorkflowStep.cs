using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class KnowledgeBaseServiceSearchWorkflowStep(
    ILogger<KnowledgeBaseServiceSearchWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    KnowledgeBaseExecutor knowledgeBaseSearchExecutor) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Knowledge Base Service Search";

    private readonly ILogger<KnowledgeBaseServiceSearchWorkflowStep> _logger = logger;
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

public partial class KnowledgeBaseServiceSearchWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => false;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => null;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(EWParameterNames.UserIntent, false),
        new(EWParameterNames.DomainsKnowledgeBaseQuery, false)
    ];

    public override async Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default)
    {
        var userIntent = inputParameters.FirstOrDefault(p => p.Name == EWParameterNames.UserIntent).RawValue ?? string.Empty;
        var queriesValue = inputParameters.FirstOrDefault(p => p.Name == EWParameterNames.DomainsKnowledgeBaseQuery).RawValue ?? string.Empty;

        KnowledgeBaseQueryInput queryInput = new()
        {
            Collections = ["domains"],
            UserIntent = userIntent,
            Queries = []
        };

        var brcOutput = await _knowledgeBaseSearchExecutor.QueryAsync(queryInput, cancellationToken);

        return new WorkflowStepResultRecord
        {
            OutputParameters = new Dictionary<string, string?>
            {
                { EWParameterNames.KnowledgeBaseQueryResults, string.Join(", ", brcOutput.Results.Select(r => r.File)) }
            }
        };
    }
}
