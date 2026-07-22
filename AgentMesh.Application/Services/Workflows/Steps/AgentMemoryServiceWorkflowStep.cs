using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;
using AgentMesh.Application.Models.Workflows;

namespace AgentMesh.Application.Services.Workflows.Steps;

public class AgentMemoryServiceWorkflowStep(
    ILogger<AgentMemoryServiceWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    AgentMemoryExecutor agentMemoryRetriever) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Agent Memory Service";

    private readonly ILogger<AgentMemoryServiceWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly AgentMemoryExecutor _agentMemoryRetriever = agentMemoryRetriever;

    public async Task ExecuteAgentMemoryServiceAsync(CodeModeWorkflowState state)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Agent Memory Service...");

        var queriesList = state.PastMemoriesQuery.Select(s => s.Memory).ToList();

        var agentInput = new AgentMemoryRetrieverInput
        {
            Query = string.Join(", ", queriesList)
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart(WorkflowStepDisplayName, agentInput.ToDictionary());

        var brcOutput = await _agentMemoryRetriever.GetAsync(agentInput);

        var retrievedMemories = brcOutput.Items.ToList();
        state.PastMemoriesQueryResults = state.PastMemoriesQueryResults.Concat(retrievedMemories).ToList();

        state.AddStepUsage(WorkflowStepDisplayName, stopwatch.Elapsed, false);

        var notifyDictionary = brcOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd(WorkflowStepDisplayName, notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteAgentMemoryServiceAsync(stateObject);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

