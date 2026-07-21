using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
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

        var agentInput = new DocumentationAgentInput
        {
            UserRequest = state.CanonicalizedUserRequest ?? state.UserRequest,
            AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
            KnowledgeBaseDocumentsContent = WorkflowExecutorFormatting.SerializeDocumentation(state.DomainsKnowledgeBaseDocumentsContent),
            LanguageOfTheUser = state.LanguageOfTheUser
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Documentation Agent", agentInput.ToDictionary());

        var output = await _documentationAgent.ExecuteAsync(agentInput, cancellationToken);
        state.DocumentationContent = output.Content;

        state.AddTokenUsage(DocumentationAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Documentation Agent");

        var notifyDictionary = output.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Documentation Agent", notifyDictionary);
    }
}

