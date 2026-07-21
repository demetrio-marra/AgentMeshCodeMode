using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Reranker;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class APIRerankerWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IRerankerAgent rerankerAgent)
{
    public const string UsageAgentName = "Reranker(APIs)";

    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IRerankerAgent _rerankerAgent = rerankerAgent;

    public async Task ExecuteAPIRerankerAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var candidates = state.APISKnowledgeBaseQueryResults.Results.ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging API Reranker Agent...");

        await _workflowProgressNotifier.NotifyWorkflowStepStart("API Reranker Agent", new Dictionary<string, string>
        {
            { "Intent", state.Intent },
            { "IntentCategory", state.IntentCategory.ToString() },
            { "ConversationTopic", state.ConversationTopic },
            { "UserRequestedActions", state.UserRequestedActions.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserRequestedActions) : "(No actions)" },
            { "UserProvidedData", state.UserProvidedData.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserProvidedData) : "(No data)" },
            { "UserPreferences", state.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserPreferences) : "(No preferences)" },
            { "Candidates", WorkflowExecutorFormatting.ToBulletList(candidates.Select(c => $"{c.Title} | {c.File} | relevance:{(c.Relevance?.ToString("0.####") ?? "n/a")}")) }
        });

        var rerankerOutput = await _rerankerAgent.ExecuteAsync(new RerankerAgentInput
        {
            StructuredUserRequest = state.NewCanonicalizedStructuredUserRequest ?? state.NewStructuredUserRequest,
            QueryResults = candidates
        }, cancellationToken);

        state.APISKnowledgeBaseQueryResults = new KnowledgeBaseQueryResult
        {
            Results = rerankerOutput.QueryResults
        };

        state.AddTokenUsage(UsageAgentName, rerankerOutput.InputTokenCount, rerankerOutput.OutputTokenCount, stopwatch.Elapsed, "API Reranker Agent");

        await _workflowProgressNotifier.NotifyWorkflowStepEnd("API Reranker Agent", new Dictionary<string, string>
        {
            { "RerankedItems", state.APISKnowledgeBaseQueryResults.Results.Any() ? WorkflowExecutorFormatting.ToBulletList(state.APISKnowledgeBaseQueryResults.Results.Select(c => $"{c.Title} | {c.File}")) : "(No valuable items)" },
            { "ResultsCount", state.APISKnowledgeBaseQueryResults.Results.Count().ToString() },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        });
    }
}
