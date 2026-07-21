using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Reranker;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class RerankerWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IRerankerAgent rerankerAgent)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IRerankerAgent _rerankerAgent = rerankerAgent;

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
            StructuredUserRequest = state.NewCanonicalizedStructuredUserRequest ?? state.NewStructuredUserRequest,
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
}
