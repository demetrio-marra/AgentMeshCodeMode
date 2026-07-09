using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.IntentCanonicalization;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class IntentCanonicalizationWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IIntentCanonicalizationAgent intentCanonicalizationAgent,
    CodeModeWorkflowConfiguration workflowConfiguration)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IIntentCanonicalizationAgent _intentCanonicalizationAgent = intentCanonicalizationAgent;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;

    public async Task ExecuteIntentCanonicalizationAsync(CodeModeWorkflowState state)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Intent Canonicalization Agent...");

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Intent Canonicalization Agent", new Dictionary<string, string>
        {
            { "Intent", state.ClassifiedUserRequest.Intent ?? "(No intent)" },
            { "EntitiesByDomain", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(e => $"[{kvp.Key}] {e}"))) : "(No entities)" },
            { "DomainsKnowledgeBaseDocumentsContent", state.DomainsKnowledgeBaseDocumentsContent.Any() ? WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseDocumentsContent.Select(f => f.File)) : "(No knowledge base results)" },
            { "NonCanonicalizedQueries", state.DomainsKnowledgeBaseQuery.Any() ? WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseQuery) : "(No non-canonicalized queries)" },
            { "LanguageOfKnowledgeBase", _workflowConfiguration.LanguageOfKnowledgeBase }
        });

        var output = await _intentCanonicalizationAgent.ExecuteAsync(new IntentCanonicalizationAgentInput
        {
            Intent = state.ClassifiedUserRequest.Intent ?? string.Empty,
            UserIntentCategory = state.ClassifiedUserRequest.IntentCategory,
            EntitiesByDomain = state.ClassifiedUserRequest.EntitiesByDomain,
            SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
            DomainDocumentationContents = state.DomainsKnowledgeBaseDocumentsContent.Any() ? WorkflowExecutorFormatting.SerializeDocumentation(state.DomainsKnowledgeBaseDocumentsContent) : "(No knowledge base results)",
            NonCanonicalizedQueries = state.DomainsKnowledgeBaseQuery,
            LanguageOfKnowledgeBase = _workflowConfiguration.LanguageOfKnowledgeBase
        });

        state.CanonicalizedIntent = output.DomainedIntent;
        state.CanonicalizedIntentCategory = output.CanonicalizedIntentCategory;
        state.CanonicalizedAPIQueries = output.CanonicalizedAPIQueries;
        state.AddTokenUsage(IntentCanonicalizationAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Intent Canonicalization Agent");

        var notifyDictionary = new Dictionary<string, string>
        {
            { "CanonicalizedIntent", state.CanonicalizedIntent },
            { "CanonicalizedIntentCategory", state.CanonicalizedIntentCategory.ToString() },
            { "CanonicalizedAPIQueries", state.CanonicalizedAPIQueries.Any() ? WorkflowExecutorFormatting.ToBulletList(state.CanonicalizedAPIQueries) : "(No canonicalized API queries)" },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Intent Canonicalization Agent", notifyDictionary);
    }
}

