using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.TechnicalAnalyst;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Models.Workflows.Parameters;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class TechnicalAnalystWorkflowStep(
    ILogger<TechnicalAnalystWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    TechnicalAnalystAgent technicalAnalystAgent,
    IAgentSelector agentSelector) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Technical Analyst";

    private readonly ILogger<TechnicalAnalystWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly TechnicalAnalystAgent _technicalAnalystAgent = technicalAnalystAgent;
    private readonly IAgentSelector _agentSelector = agentSelector;

    public async Task ExecuteTechnicalAnalystAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Technical Analyst Agent...");

        var agentInput = new TechnicalAnalystAgentInput
        {
            Intent = state.Intent,
            ConversationTopic = state.ConversationTopic,
            BusinessRequirements = state.BusinessRequirements ?? string.Empty,
            UserRequestedActions = state.UserRequestedActions,
            UserProvidedData = state.UserProvidedData,
            UserPreferences = state.UserPreferences,
            AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
            KnowledgeBaseDocumentsContent = WorkflowExecutorFormatting.SerializeDocumentation(state.KnowledgeBaseAPIDocumentsContent)
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Technical Analyst Agent", agentInput.ToDictionary());

        var technicalAnalystOutput = await _technicalAnalystAgent.ExecuteAsync(agentInput, cancellationToken);

        state.ShouldEngageCoder = state.ShouldEngageCoder && !technicalAnalystOutput.RequestRejected;
        state.TechnicalSpecification = technicalAnalystOutput.TechnicalSpecification;
        state.TechnicalAnalystRejected = technicalAnalystOutput.RequestRejected;
        state.TechnicalAnalystRejectReasons = technicalAnalystOutput.ReasonOfRejection;
        state.SelectedAPIsFileLocations = technicalAnalystOutput.SelectedAPIsFileLocations;
        state.AddTokenUsage(TechnicalAnalystAgentConfiguration.AgentName, technicalAnalystOutput.InputTokenCount, technicalAnalystOutput.OutputTokenCount, stopwatch.Elapsed, "Technical Analyst Agent");

        var notifyDictionary = technicalAnalystOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Technical Analyst Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteTechnicalAnalystAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

public partial class TechnicalAnalystWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => true;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => TechnicalAnalystAgentConfiguration.AgentName;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(EWParameterNames.UserIntent, false),
        new(EWParameterNames.ConversationTopic, false),
        new(EWParameterNames.BusinessRequirements, false),
        new(EWParameterNames.UserRequestedActions, false),
        new(EWParameterNames.UserProvidedData, false),
        new(EWParameterNames.UserPreferences, false),
        new(EWParameterNames.PastMemoriesQueryResults, false),
        new(EWParameterNames.KnowledgeBaseAPIDocumentsContent, true)
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
                AgentName = TechnicalAnalystAgentConfiguration.AgentName,
                InputTokens = agentOutput.InputTokens,
                OutputTokens = agentOutput.OutputTokens
            }
        };
    }
}

