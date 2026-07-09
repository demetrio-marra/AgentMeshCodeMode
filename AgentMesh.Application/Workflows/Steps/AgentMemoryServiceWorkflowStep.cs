using AgentMesh.Application.Models;
using AgentMesh.Services;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Workflows;
using AgentMesh.Models;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.QueriesCache;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class AgentMemoryServiceWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IAgentMemoryRetrieverExecutor agentMemoryRetriever,
    CodeModeWorkflowConfiguration workflowConfiguration,
    IQueriesCacheService queriesCacheService)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IAgentMemoryRetrieverExecutor _agentMemoryRetriever = agentMemoryRetriever;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;
    private readonly IQueriesCacheService _queriesCacheService = queriesCacheService;

    public async Task ExecuteAgentMemoryServiceAsync(CodeModeWorkflowState state)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Agent Memory Service...");
        await _workflowProgressNotifier.NotifyWorkflowStepStart("Agent Memory Service", new Dictionary<string, string>
        {
            { "MissingPastMemories", WorkflowExecutorFormatting.ToBulletList(state.PastMemoriesQuery) }
        });

        var queriesList = state.PastMemoriesQuery.ToList();

        var brcOutput = await _agentMemoryRetriever.ExecuteAsync(new AgentMemoryRetrieverInput
        {
            Query = string.Join(", ", queriesList)
        });

        var retrievedMemories = brcOutput.Items.ToList();
        state.PastMemoriesQueryResults = state.PastMemoriesQueryResults.Concat(retrievedMemories).ToList();

        if (_workflowConfiguration.EnableCacheService && retrievedMemories.Any())
        {
            var cacheItems = queriesList
                .Zip(retrievedMemories, (query, result) => new AgentMemoryQueriesCacheItem
                {
                    FoundQuery = query,
                    Result = result.Memory
                })
                .ToList();

            var cacheUpdateResult = await _queriesCacheService.SetMemoryCachedItemsAsync(cacheItems);

            var tokenUsageInfo = new AgentTokenUsageEntry
            {
                AgentName = "Query Cache Updater Service (Memory)",
                InputTokens = cacheUpdateResult.TotalTokens,
                OutputTokens = 0
            };
            state.AddStepUsage("Agent Memory Service", stopwatch.Elapsed, true, tokenUsageInfo);
        }
        else
        {
            state.AddStepUsage("Agent Memory Service", stopwatch.Elapsed, false);
        }

        var notifyDictionary = new Dictionary<string, string>
        {
            { "ExtractedAgentMemories", WorkflowExecutorFormatting.ToBulletList(retrievedMemories.Select(m => m.Memory)) },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Agent Memory Service", notifyDictionary);
    }
}

