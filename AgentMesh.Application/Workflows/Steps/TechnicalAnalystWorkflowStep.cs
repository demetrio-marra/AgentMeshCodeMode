using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.TechnicalAnalyst;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class TechnicalAnalystWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    ITechnicalAnalystAgent technicalAnalystAgent)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly ITechnicalAnalystAgent _technicalAnalystAgent = technicalAnalystAgent;

    public async Task ExecuteTechnicalAnalystAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Technical Analyst Agent...");
        await _workflowProgressNotifier.NotifyWorkflowStepStart("Technical Analyst Agent", new Dictionary<string, string>
        {
            { "Intent", state.Intent },
            { "BusinessRequirements", state.BusinessRequirements ?? "(No business requirements)" },
            { "UserRequestedActions", state.UserRequestedActions.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserRequestedActions) : "(No actions)" },
            { "UserProvidedData", state.UserProvidedData.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserProvidedData) : "(No data)" },
            { "UserPreferences", state.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserPreferences) : "(No user preferences)" },
            { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? WorkflowExecutorFormatting.ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" },
            { "KnowledgeBaseDocumentsContent", state.KnowledgeBaseAPIDocumentsContent.Any() ? WorkflowExecutorFormatting.ToBulletList(state.KnowledgeBaseAPIDocumentsContent.Select(d => d.File)) : "(No documents)" }
        });

        var technicalAnalystOutput = await _technicalAnalystAgent.ExecuteAsync(new TechnicalAnalystAgentInput
        {
            Intent = state.Intent,
            BusinessRequirements = state.BusinessRequirements ?? string.Empty,
            SupportingIntentInformation = state.UserRequestedActions,
            Entities = new Dictionary<string, IEnumerable<string>>(),
            UserPreferences = state.UserPreferences,
            AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
            KnowledgeBaseDocumentsContent = WorkflowExecutorFormatting.SerializeDocumentation(state.KnowledgeBaseAPIDocumentsContent)
        }, cancellationToken);

        state.ShouldEngageCoder = state.ShouldEngageCoder && !technicalAnalystOutput.RequestRejected;
        state.TechnicalSpecification = technicalAnalystOutput.TechnicalSpecification;
        state.TechnicalAnalystRejected = technicalAnalystOutput.RequestRejected;
        state.TechnicalAnalystRejectReasons = technicalAnalystOutput.ReasonOfRejection;
        state.SelectedAPIsFileLocations = technicalAnalystOutput.SelectedAPIsFileLocations;
        state.AddTokenUsage(TechnicalAnalystAgentConfiguration.AgentName, technicalAnalystOutput.InputTokenCount, technicalAnalystOutput.OutputTokenCount, stopwatch.Elapsed, "Technical Analyst Agent");
        var notifyDictionary = new Dictionary<string, string>
        {
            { "TechnicalSpecification", state.TechnicalSpecification ?? "(No technical specification)" },
            { "TechnicalAnalystRejected", state.TechnicalAnalystRejected.ToString() },
            { "TechnicalAnalystRejectReasons", state.TechnicalAnalystRejectReasons ?? "(No rejection reasons)" },
            { "SelectedAPIsFileLocations", state.SelectedAPIsFileLocations.Any() ? WorkflowExecutorFormatting.ToBulletList(state.SelectedAPIsFileLocations) : "(No selected APIs)" },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Technical Analyst Agent", notifyDictionary);
    }
}

