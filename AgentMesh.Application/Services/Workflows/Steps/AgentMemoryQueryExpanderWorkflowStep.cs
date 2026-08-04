using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.AgentMemoryQueryExpander;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class AgentMemoryQueryExpanderWorkflowStep(
    ILogger<AgentMemoryQueryExpanderWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    AgentMemoryQueryExpanderAgent agentMemoryQueryExpanderAgent,
    IAgentSelector agentSelector) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Agent Memory Query Expander";

    private readonly ILogger<AgentMemoryQueryExpanderWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly AgentMemoryQueryExpanderAgent _agentMemoryQueryExpanderAgent = agentMemoryQueryExpanderAgent;
    private readonly IAgentSelector _agentSelector = agentSelector;

    public async Task ExecuteAgentMemoryQueryExpanderAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Agent Memory Query Expander Agent...");

        var agentInput = new AgentMemoryQueryExpanderAgentInput
        {
            MemoryTopics = state.UserRequest!.MissingValues.Select(mv => new AgentMemoryItem { Memory = mv }).ToList()
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Agent Memory Query Expander Agent", agentInput.ToDictionary());

        var agentOutput = await _agentMemoryQueryExpanderAgent.ExecuteAsync(agentInput, cancellationToken);

        state.PastMemoriesQuery = agentOutput.SearchQueries.Select(q => new AgentMemoryItem { Memory = q }).ToList();

        state.AddTokenUsage(AgentMemoryQueryExpanderAgentConfiguration.AgentName, agentOutput.InputTokenCount, agentOutput.OutputTokenCount, stopwatch.Elapsed, "Agent Memory Query Expander Agent");

        var notifyDictionary = agentOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Agent Memory Query Expander Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteAgentMemoryQueryExpanderAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = true
        };
    }
}

public partial class AgentMemoryQueryExpanderWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => true;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => AgentMemoryQueryExpanderAgentConfiguration.AgentName;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(EWParameterNames.MissingValues, false)
    ];

    public override async Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default)
    {
        var agentInput = ToAgentInputParameters(inputParameters);

        var agent = _agentSelector.GetAgent(AgentName!);
        var agentOutput = await agent.ExecuteAsync(agentInput, cancellationToken);

        return new WorkflowStepResultRecord
        {
            OutputParameters = agentOutput.OutputParameters.ToDictionary(p => p.Name, p => p.Value),
            AgentTokenUsageEntry = new AgentTokenUsageEntry
            {
                AgentName = AgentMemoryQueryExpanderAgentConfiguration.AgentName,
                InputTokens = agentOutput.InputTokens,
                OutputTokens = agentOutput.OutputTokens
            }
        };
    }
}
