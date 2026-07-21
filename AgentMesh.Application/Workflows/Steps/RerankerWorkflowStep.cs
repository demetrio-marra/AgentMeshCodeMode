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

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Reranker Agent", new Dictionary<string, string>
        {
            { "Intent", state.Intent },
            { "IntentCategory", state.IntentCategory.ToString() },
            { "ConversationTopic", state.ConversationTopic },
            { "UserRequestedActions", state.UserRequestedActions.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserRequestedActions) : "(No actions)" },
            { "UserProvidedData", state.UserProvidedData.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserProvidedData) : "(No data)" },
            { "UserPreferences", state.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserPreferences) : "(No preferences)" },
            //{ "MissingValues", state.MissingValues.Any() ? WorkflowExecutorFormatting.ToBulletList(state.MissingValues) : "(No missing values)" },
            { "Candidates", WorkflowExecutorFormatting.ToBulletList(candidates.Select(c => $"{c.Title} | {c.File} | relevance:{(c.Relevance?.ToString("0.####") ?? "n/a")}")) }
        });

        var rerankerOutput = await _rerankerAgent.ExecuteAsync(new RerankerAgentInput
        {
            StructuredUserRequest = state.NewCanonicalizedStructuredUserRequest ?? state.NewStructuredUserRequest,
            QueryResults = candidates
        }, cancellationToken);

        state.DomainsKnowledgeBaseQueryResults = new KnowledgeBaseQueryResult
        {
            Results = rerankerOutput.QueryResults
        };

        state.AddTokenUsage(RerankerAgentConfiguration.AgentName, rerankerOutput.InputTokenCount, rerankerOutput.OutputTokenCount, stopwatch.Elapsed, "Reranker Agent");

        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Reranker Agent", new Dictionary<string, string>
        {
            { "RerankedItems", state.DomainsKnowledgeBaseQueryResults.Results.Any() ? WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseQueryResults.Results.Select(c => $"{c.Title} | {c.File}")) : "(No valuable items)" },
            { "ResultsCount", state.DomainsKnowledgeBaseQueryResults.Results.Count().ToString() },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        });
    }
}
