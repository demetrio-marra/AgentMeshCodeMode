using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.Documentation;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class DocumentationWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IDocumentationAgent documentationAgent)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IDocumentationAgent _documentationAgent = documentationAgent;

    public async Task ExecuteDocumentationAgentAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Documentation Agent...");
        var enrichedUserRequest = state.CanonicalizedIntent;
        await _workflowProgressNotifier.NotifyWorkflowStepStart("Documentation Agent", new Dictionary<string, string>
        {
            { "EnrichedUserRequest", enrichedUserRequest },
            { "Intent", state.CanonicalizedIntent },
            { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
            { "Entities", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(v => $"[{kvp.Key}] {v}"))) : "(No entities)" },
            { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
            { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? WorkflowExecutorFormatting.ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" },
            { "DomainsKnowledgeBaseDocumentsContent", state.DomainsKnowledgeBaseDocumentsContent.Any() ? WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseDocumentsContent.Select(d => d.File)) : "(No documents)" }
        });

        var serializedDocumentation = WorkflowExecutorFormatting.SerializeDocumentation(state.DomainsKnowledgeBaseDocumentsContent);

        var output = await _documentationAgent.ExecuteAsync(new DocumentationAgentInput
        {
            EnrichedUserRequest = enrichedUserRequest,
            Intent = state.CanonicalizedIntent,
            SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
            Entities = state.ClassifiedUserRequest.EntitiesByDomain,
            UserPreferences = state.ClassifiedUserRequest.UserPreferences,
            AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
            KnowledgeBaseDocumentsContent = serializedDocumentation
        }, cancellationToken);
        state.DocumentationContent = output.Content;
        state.AddTokenUsage(DocumentationAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Documentation Agent");
        var notifyDictionary = new Dictionary<string, string>
        {
            { "Content", state.DocumentationContent ?? "(No documentation content)" },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Documentation Agent", notifyDictionary);
    }
}

