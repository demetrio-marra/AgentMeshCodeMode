using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.TechnicalAnalyst;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Executors;

public class TechnicalAnalystWorkflowExecutor(
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
            { "Intent", state.CanonicalizedIntent },
            { "BusinessRequirements", state.BusinessRequirements ?? "(No business requirements)" },
            { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
            { "Entities", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(v => $"[{kvp.Key}] {v}"))) : "(No entities)" },
            { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
            { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? WorkflowExecutorFormatting.ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" },
            { "KnowledgeBaseDocumentsContent", state.KnowledgeBaseAPIDocumentsContent.Any() ? WorkflowExecutorFormatting.ToBulletList(state.KnowledgeBaseAPIDocumentsContent.Select(d => d.File)) : "(No documents)" }
        });

        var technicalAnalystOutput = await _technicalAnalystAgent.ExecuteAsync(new TechnicalAnalystAgentInput
        {
            Intent = state.CanonicalizedIntent,
            BusinessRequirements = state.BusinessRequirements ?? string.Empty,
            SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
            Entities = state.ClassifiedUserRequest.EntitiesByDomain,
            UserPreferences = state.ClassifiedUserRequest.UserPreferences,
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
            { "SelectedAPIsFileLocations", state.SelectedAPIsFileLocations.Any() ? string.Join(", ", state.SelectedAPIsFileLocations) : "(No selected APIs)" },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Technical Analyst Agent", notifyDictionary);
    }
}

