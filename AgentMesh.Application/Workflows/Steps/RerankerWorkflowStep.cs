using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Models.Reranker;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Workflows.Steps;

public class RerankerWorkflowStep(
    ILogger<RerankerWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    RerankerAgent rerankerAgent) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Reranker";

    private readonly ILogger<RerankerWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly RerankerAgent _rerankerAgent = rerankerAgent;

    public async Task ExecuteRerankerAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var candidates = state.DomainsKnowledgeBaseQueryResults.Results.ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Reranker Agent...");

        var agentInput = new RerankerAgentInput
        {
            StructuredUserRequest = state.CanonicalizedUserRequest ?? state.UserRequest,
            QueryResults = candidates
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Reranker Agent", agentInput.ToDictionary());

        var rerankerOutput = await _rerankerAgent.ExecuteAsync(agentInput, cancellationToken);

        state.DomainsKnowledgeBaseQueryResults = new KnowledgeBaseQueryResult
        {
            Results = rerankerOutput.QueryResults
        };

        state.AddTokenUsage(RerankerAgentConfiguration.AgentName, rerankerOutput.InputTokenCount, rerankerOutput.OutputTokenCount, stopwatch.Elapsed, "Reranker Agent");

        var notifyDictionary = rerankerOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Reranker Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteRerankerAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}
