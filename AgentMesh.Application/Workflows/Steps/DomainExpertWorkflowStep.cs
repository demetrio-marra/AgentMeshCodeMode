using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Application.Models.DomainExpert;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class DomainExpertWorkflowStep(
    ILogger<DomainExpertWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    DomainExpertAgent domainExpertAgent) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Domain Expert";

    private readonly ILogger<DomainExpertWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly DomainExpertAgent _domainExpertAgent = domainExpertAgent;

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

