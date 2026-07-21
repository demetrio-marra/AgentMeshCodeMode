using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.FunctionalAnalyst;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class FunctionalAnalystWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IFunctionalAnalystAgent functionalAnalystAgent,
    CodeModeWorkflowConfiguration workflowConfiguration)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IFunctionalAnalystAgent _functionalAnalystAgent = functionalAnalystAgent;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;

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
}

