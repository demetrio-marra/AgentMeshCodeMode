using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.DomainExpert;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class DomainExpertWorkflowStep(
    ILogger<DomainExpertWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    DomainExpertAgent domainExpertAgent,
    IAgentSelector agentSelector) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Domain Expert";

    private readonly ILogger<DomainExpertWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly DomainExpertAgent _domainExpertAgent = domainExpertAgent;
    private readonly IAgentSelector _agentSelector = agentSelector;

    public async Task ExecuteDomainExpertAgentAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Domain Expert Agent...");

        var agentInput = new DomainExpertAgentInput
        {
            Intent = state.Intent,
            ConversationTopic = state.ConversationTopic,
            UserRequestedActions = state.UserRequestedActions,
            UserProvidedData = state.UserProvidedData,
            UserPreferences = state.UserPreferences,
            AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
            KnowledgeBaseDocumentsContent = WorkflowExecutorFormatting.SerializeDocumentation(state.DomainsKnowledgeBaseDocumentsContent),
            DataToComment = state.SandboxResult ?? string.Empty,
            LanguageOfTheUser = state.LanguageOfTheUser
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Domain Expert Agent", agentInput.ToDictionary());

        var output = await _domainExpertAgent.ExecuteAsync(agentInput, cancellationToken);

        state.DomainExpertOutput = output.DomainExpertComment;
        state.AddTokenUsage(DomainExpertAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Domain Expert Agent");

        var notifyDictionary = output.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Domain Expert Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteDomainExpertAgentAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

public partial class DomainExpertWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => true;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => "DomainExpert";

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(EWParameterNames.UserIntent, false),
        new(EWParameterNames.ConversationTopic, false),
        new(EWParameterNames.UserRequestedActions, false),
        new(EWParameterNames.UserProvidedData, false),
        new(EWParameterNames.UserPreferences, false),
        new(EWParameterNames.PastMemoriesQueryResults, false),
        new(EWParameterNames.DomainsKnowledgeBaseDocumentsContent, true),
        new(EWParameterNames.SandboxResult, true),
        new(EWParameterNames.LanguageOfTheUser, false)
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
                AgentName = DomainExpertAgentConfiguration.AgentName,
                InputTokens = agentOutput.InputTokens,
                OutputTokens = agentOutput.OutputTokens
            }
        };
    }

}
