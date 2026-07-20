using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.RequestCanonicalization;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class RequestCanonicalizationWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IRequestCanonicalizationAgent requestCanonicalizationAgent,
    CodeModeWorkflowConfiguration workflowConfiguration)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IRequestCanonicalizationAgent _requestCanonicalizationAgent = requestCanonicalizationAgent;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;
    private const string QmdQueryTypesFileName = "QMDQueryTypes.md";

    public async Task ExecuteRequestCanonicalizationAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Request Canonicalization Agent...");

        var structuredUserRequest = state.NewStructuredUserRequest ?? new AgentMesh.Models.RequestAnalysis.StructuredUserRequest();
        var domainsKnowledgeBaseDocumentsContent = state.DomainsKnowledgeBaseDocumentsContent.Any()
            ? WorkflowExecutorFormatting.SerializeDocumentation(state.DomainsKnowledgeBaseDocumentsContent)
            : "(No knowledge base results)";

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Request Canonicalization Agent", new Dictionary<string, string>
        {
            { "StructuredUserRequest.Intent", structuredUserRequest.Intent },
            { "StructuredUserRequest.ConversationTopic", structuredUserRequest.ConversationTopic ?? "(No conversation topic)" },
            { "StructuredUserRequest.UserRequestedActions", structuredUserRequest.UserRequestedActions.Any() ? WorkflowExecutorFormatting.ToBulletList(structuredUserRequest.UserRequestedActions) : "(No requested actions)" },
            { "StructuredUserRequest.UserProvidedData", structuredUserRequest.UserProvidedData.Any() ? WorkflowExecutorFormatting.ToBulletList(structuredUserRequest.UserProvidedData) : "(No provided data)" },
            { "StructuredUserRequest.UserPreferences", structuredUserRequest.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(structuredUserRequest.UserPreferences) : "(No user preferences)" },
            { "DomainsKnowledgeBaseQuery", state.DomainsKnowledgeBaseQuery.Any() ? WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseQuery) : "(No queries)" },
            { "DomainsKnowledgeBaseDocumentsContent (files)", state.DomainsKnowledgeBaseDocumentsContent.Any() ? WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseDocumentsContent.Select(s => s.File)) : "(No knowledge base results)" },
            { "LanguageOfKnowledgeBase", _workflowConfiguration.LanguageOfKnowledgeBase }
        });

        var output = await _requestCanonicalizationAgent.ExecuteAsync(new RequestCanonicalizationAgentInput
        {
            StructuredUserRequest = structuredUserRequest,
            DomainsKnowledgeBaseQuery = state.DomainsKnowledgeBaseQuery,
            DomainsKnowledgeBaseDocumentsContent = domainsKnowledgeBaseDocumentsContent,
            LanguageOfKnowledgeBase = _workflowConfiguration.LanguageOfKnowledgeBase,
            QmdQueryTypesReference = LoadQmdQueryTypesReference()
        }, cancellationToken);

        state.NewCanonicalizedStructuredUserRequest = output.CanonicalizedStructuredUserRequest;
        state.CanonicalizedIntentCategory = output.CanonicalizedIntentCategory;
        state.DomainsKnowledgeBaseQuery = output.CanonicalizedDomainsKnowledgeBaseQuery;

        state.AddTokenUsage(RequestCanonicalizationAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Request Canonicalization Agent");

        var canonicalizedStructuredUserRequest = state.NewCanonicalizedStructuredUserRequest ?? new AgentMesh.Models.RequestAnalysis.StructuredUserRequest();
        var notifyDictionary = new Dictionary<string, string>
        {
            { "CanonicalizedStructuredUserRequest.Intent", canonicalizedStructuredUserRequest.Intent },
            { "CanonicalizedStructuredUserRequest.ConversationTopic", canonicalizedStructuredUserRequest.ConversationTopic ?? "(No conversation topic)" },
            { "CanonicalizedStructuredUserRequest.UserRequestedActions", canonicalizedStructuredUserRequest.UserRequestedActions.Any() ? WorkflowExecutorFormatting.ToBulletList(canonicalizedStructuredUserRequest.UserRequestedActions) : "(No requested actions)" },
            { "CanonicalizedStructuredUserRequest.UserProvidedData", canonicalizedStructuredUserRequest.UserProvidedData.Any() ? WorkflowExecutorFormatting.ToBulletList(canonicalizedStructuredUserRequest.UserProvidedData) : "(No provided data)" },
            { "CanonicalizedStructuredUserRequest.UserPreferences", canonicalizedStructuredUserRequest.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(canonicalizedStructuredUserRequest.UserPreferences) : "(No user preferences)" },
            { "CanonicalizedIntentCategory", state.CanonicalizedIntentCategory.ToString() },
            { "CanonicalizedDomainsKnowledgeBaseQuery", state.DomainsKnowledgeBaseQuery.Any() ? WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseQuery) : "(No canonicalized queries)" },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Request Canonicalization Agent", notifyDictionary);
    }

    private string? LoadQmdQueryTypesReference()
    {
        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Prompts", QmdQueryTypesFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Prompts", QmdQueryTypesFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "AgentMeshCLI", "Prompts", QmdQueryTypesFileName)
        };

        foreach (var candidatePath in candidatePaths)
        {
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            return File.ReadAllText(candidatePath);
        }

        _logger.LogWarning("Unable to locate QMD query types prompt file '{FileName}' in expected paths.", QmdQueryTypesFileName);
        return null;
    }
}
