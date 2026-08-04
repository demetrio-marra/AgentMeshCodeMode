using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.FunctionalAnalyst;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Models.Workflows.Parameters;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class FunctionalAnalystWorkflowStep(
    ILogger<FunctionalAnalystWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    FunctionalAnalystAgent functionalAnalystAgent,
    CodeModeWorkflowConfiguration workflowConfiguration,
    IAgentSelector agentSelector) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Functional Analyst";

    private readonly ILogger<FunctionalAnalystWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly FunctionalAnalystAgent _functionalAnalystAgent = functionalAnalystAgent;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;
    private readonly IAgentSelector _agentSelector = agentSelector;

    public async Task ExecuteFunctionalAnalystAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Functional Analyst Agent...");

        var agentInput = new FunctionalAnalystAgentInput
        {
            Intent = state.Intent,
            ConversationTopic = state.ConversationTopic,
            UserRequestedActions = state.UserRequestedActions,
            UserProvidedData = state.UserProvidedData,
            UserPreferences = state.UserPreferences,
            AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
            KnowledgeBaseDocumentsContent = WorkflowExecutorFormatting.SerializeDocumentation(state.DomainsKnowledgeBaseDocumentsContent),
            DoNotComment = _workflowConfiguration.EnableDomainExpert
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Functional Analyst Agent", agentInput.ToDictionary());

        var functionalAnalystOutput = await _functionalAnalystAgent.ExecuteAsync(agentInput, cancellationToken);

        state.ShouldEngageCoder = !functionalAnalystOutput.RequestRejected;
        state.BusinessRequirements = functionalAnalystOutput.BusinessRequirements;
        state.FunctionalAnalystRejected = functionalAnalystOutput.RequestRejected;
        state.FunctionalAnalystRejectReasons = functionalAnalystOutput.ReasonOfRejection;
        state.AddTokenUsage(FunctionalAnalystAgentConfiguration.AgentName, functionalAnalystOutput.InputTokenCount, functionalAnalystOutput.OutputTokenCount, stopwatch.Elapsed, "Functional Analyst Agent");

        var notifyDictionary = functionalAnalystOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Functional Analyst Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteFunctionalAnalystAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

public partial class FunctionalAnalystWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => true;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => FunctionalAnalystAgentConfiguration.AgentName;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(EWParameterNames.UserIntent, false),
        new(EWParameterNames.ConversationTopic, false),
        new(EWParameterNames.UserRequestedActions, false),
        new(EWParameterNames.UserProvidedData, false),
        new(EWParameterNames.UserPreferences, false),
        new(EWParameterNames.PastMemoriesQueryResults, false),
        new(EWParameterNames.DomainsKnowledgeBaseDocumentsContent, true)
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
                AgentName = FunctionalAnalystAgentConfiguration.AgentName,
                InputTokens = agentOutput.InputTokens,
                OutputTokens = agentOutput.OutputTokens
            }
        };
    }
}

