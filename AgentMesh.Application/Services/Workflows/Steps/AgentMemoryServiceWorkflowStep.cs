using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;
using AgentMesh.Application.Models.Workflows;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class AgentMemoryServiceWorkflowStep(
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


public partial class AgentMemoryServiceWorkflowStep : IEasyWorkflowStep
{
    public string Name { get => WorkflowStepDisplayName; }
    public bool IsAgentic { get => false; }
    public string? AgentName { get => null; }
    public bool IsInputStep { get => false; }
    public bool IsOutputStep { get => false; }


    public async Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default)
    {
        var queriesList = inputParameters.FirstOrDefault(n => n.Name == CodeModeWorkflowParametersFactory.PastMemoriesQueryParameterName).RawValue;
        if (string.IsNullOrEmpty(queriesList))
        {
            return await Task.FromResult(new WorkflowStepResultRecord
            {
                OutputParameters = new Dictionary<string, string?>()
            });
        }
        var agentInput = new AgentMemoryRetrieverInput
        {
            Query = string.Join(", ", queriesList)
        };

        var brcOutput = await _agentMemoryRetriever.GetAsync(agentInput);

        var retrievedMemories = brcOutput.Items.ToList();
        
        return new WorkflowStepResultRecord
        {
            OutputParameters = new Dictionary<string, string?>
            {
                { CodeModeWorkflowParametersFactory.PastMemoriesQueryResultsParameterName, string.Join(", ", retrievedMemories) }
            }
        };
    }

    public IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames { get => [
        new(CodeModeWorkflowParametersFactory.PastMemoriesQueryParameterName, false)
        ]; }
}

