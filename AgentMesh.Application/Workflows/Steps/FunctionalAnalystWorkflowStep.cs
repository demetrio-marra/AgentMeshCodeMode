using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
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
        await _workflowProgressNotifier.NotifyWorkflowStepStart("Functional Analyst Agent", new Dictionary<string, string>
        {
            { "Intent", state.CanonicalizedIntent },
            { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
            { "Entities", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(v => $"[{kvp.Key}] {v}"))) : "(No entities)" },
            { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
            { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? WorkflowExecutorFormatting.ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" },
            { "KnowledgeBaseDocumentsContent", state.DomainsKnowledgeBaseDocumentsContent.Any() ? WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseDocumentsContent.Select(d => d.File)) : "(No documents)" }
        });

        var functionalAnalystOutput = await _functionalAnalystAgent.ExecuteAsync(new FunctionalAnalystAgentInput
        {
            Intent = state.CanonicalizedIntent,
            SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
            Entities = state.ClassifiedUserRequest.EntitiesByDomain,
            UserPreferences = state.ClassifiedUserRequest.UserPreferences,
            AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
            KnowledgeBaseDocumentsContent = WorkflowExecutorFormatting.SerializeDocumentation(state.DomainsKnowledgeBaseDocumentsContent),
            DoNotComment = _workflowConfiguration.EnableDomainExpert
        }, cancellationToken);

        state.ShouldEngageCoder = !functionalAnalystOutput.RequestRejected;
        state.BusinessRequirements = functionalAnalystOutput.BusinessRequirements;
        state.FunctionalAnalystRejected = functionalAnalystOutput.RequestRejected;
        state.FunctionalAnalystRejectReasons = functionalAnalystOutput.ReasonOfRejection;
        state.AddTokenUsage(FunctionalAnalystAgentConfiguration.AgentName, functionalAnalystOutput.InputTokenCount, functionalAnalystOutput.OutputTokenCount, stopwatch.Elapsed, "Functional Analyst Agent");
        var notifyDictionary = new Dictionary<string, string>
        {
            { "BusinessRequirements", state.BusinessRequirements ?? "(No business requirements)" },
            { "FunctionalAnalystRejected", state.FunctionalAnalystRejected.ToString() },
            { "FunctionalAnalystRejectReasons", state.FunctionalAnalystRejectReasons ?? "(No rejection reasons)" },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Functional Analyst Agent", notifyDictionary);
    }
}

