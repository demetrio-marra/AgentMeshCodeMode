using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.QueryExpander;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class QueryExpanderWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IQueryExpanderAgent queryExpanderAgent,
    CodeModeWorkflowConfiguration workflowConfiguration)
{
    private const string QmdQueryTypesFileName = "QMDQueryTypes.md";

    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IQueryExpanderAgent _queryExpanderAgent = queryExpanderAgent;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;

    public async Task ExecuteQueryExpanderAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var sr = state.NewStructuredUserRequest!;

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Query Expander Agent...");
        await _workflowProgressNotifier.NotifyWorkflowStepStart("Query Expander Agent", new Dictionary<string, string>
        {
            { "Intent", sr.Intent },
            { "IntentCategory", sr.IntentCategory.ToString() },
            { "ConversationTopic", sr.ConversationTopic ?? "(No topic)" },
            { "UserRequestedActions", sr.UserRequestedActions.Any() ? WorkflowExecutorFormatting.ToBulletList(sr.UserRequestedActions) : "(No actions)" },
            { "UserProvidedData", sr.UserProvidedData.Any() ? WorkflowExecutorFormatting.ToBulletList(sr.UserProvidedData) : "(No data)" },
            { "UserPreferences", sr.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(sr.UserPreferences) : "(No preferences)" },
            { "MissingValues", sr.MissingValues.Any() ? WorkflowExecutorFormatting.ToBulletList(sr.MissingValues) : "(No missing values)" }
        });

        var queryExpanderOutput = await _queryExpanderAgent.ExecuteAsync(new QueryExpanderAgentInput
        {
            StructuredUserRequest = sr,
            GenerateHydeQueries = sr.IntentCategory == AgentMesh.Models.RequestAnalysis.UserIntentCategory.Documentation,
            QmdQueryTypesReference = LoadQmdQueryTypesReference()
        }, cancellationToken);

        // filter also on return
        var searchQueries = queryExpanderOutput.SearchQueries.ToList();
        if (sr.IntentCategory != AgentMesh.Models.RequestAnalysis.UserIntentCategory.Documentation)
        {
            searchQueries = searchQueries.Where(q => q.SearchType != AgentMesh.Models.KnowledgeBase.KnowledgeBaseQuerySearchType.HypotheticalDocument).ToList();
        }

        state.DomainsKnowledgeBaseQuery = searchQueries;
        state.AddTokenUsage(QueryExpanderAgentConfiguration.AgentName, queryExpanderOutput.InputTokenCount, queryExpanderOutput.OutputTokenCount, stopwatch.Elapsed, "Query Expander Agent");

        var notifyDictionary = new Dictionary<string, string>
        {
            { "SearchQueries", state.DomainsKnowledgeBaseQuery.Any() ? WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseQuery.Select(q => q.ToString())) : "(No queries generated)" },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Query Expander Agent", notifyDictionary);
    }

    private string? LoadQmdQueryTypesReference()
    {
        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Prompts", QmdQueryTypesFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Prompts", QmdQueryTypesFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "AgentMeshCLI", "Prompts", QmdQueryTypesFileName)
        };

        foreach (var candidatePath in candidatePaths)
        {
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            return File.ReadAllText(candidatePath);
        }

        _logger.LogWarning("Unable to locate QMD query types prompt file '{FileName}' in expected paths.", QmdQueryTypesFileName);
        return null;
    }
}
