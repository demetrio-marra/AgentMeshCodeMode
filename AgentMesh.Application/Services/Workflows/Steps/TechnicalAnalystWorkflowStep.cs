using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Models.TechnicalAnalyst;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Application.Models.Workflows;

namespace AgentMesh.Application.Services.Workflows.Steps;

public class TechnicalAnalystWorkflowStep(
    ILogger<TechnicalAnalystWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    TechnicalAnalystAgent technicalAnalystAgent) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Technical Analyst";

    private readonly ILogger<TechnicalAnalystWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly TechnicalAnalystAgent _technicalAnalystAgent = technicalAnalystAgent;

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

