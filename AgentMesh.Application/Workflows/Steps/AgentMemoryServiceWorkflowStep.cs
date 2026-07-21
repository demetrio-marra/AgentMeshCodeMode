using AgentMesh.Application.Models;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Models.AgentMemory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class AgentMemoryServiceWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    AgentMemoryExecutor agentMemoryRetriever)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly AgentMemoryExecutor _agentMemoryRetriever = agentMemoryRetriever;

    public async Task ExecuteAgentMemoryServiceAsync(CodeModeWorkflowState state)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Agent Memory Service...");

        var queriesList = state.PastMemoriesQuery.ToList();

        var agentInput = new AgentMemoryRetrieverInput
        {
            Query = string.Join(", ", queriesList)
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Agent Memory Service", agentInput.ToDictionary());

        var brcOutput = await _agentMemoryRetriever.GetAsync(agentInput);

        var retrievedMemories = brcOutput.Items.ToList();
        state.PastMemoriesQueryResults = state.PastMemoriesQueryResults.Concat(retrievedMemories).ToList();

        state.AddStepUsage("Agent Memory Service", stopwatch.Elapsed, false);

        var notifyDictionary = brcOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Agent Memory Service", notifyDictionary);
    }
}

