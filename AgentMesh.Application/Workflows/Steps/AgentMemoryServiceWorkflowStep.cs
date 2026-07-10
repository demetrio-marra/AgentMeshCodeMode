using AgentMesh.Application.Models;
using AgentMesh.Services;
using AgentMesh.Application.Contracts;
using AgentMesh.Models.AgentMemory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class AgentMemoryServiceWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IAgentMemoryRetrieverExecutor agentMemoryRetriever)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IAgentMemoryRetrieverExecutor _agentMemoryRetriever = agentMemoryRetriever;

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
        
        state.AddStepUsage("Agent Memory Service", stopwatch.Elapsed, false);

        var notifyDictionary = new Dictionary<string, string>
        {
            { "ExtractedAgentMemories", WorkflowExecutorFormatting.ToBulletList(retrievedMemories.Select(m => m.Memory)) },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Agent Memory Service", notifyDictionary);
    }
}

