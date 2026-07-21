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

        var agentInput = new RequestCanonicalizationAgentInput
        {
            StructuredUserRequest = structuredUserRequest,
            DomainsKnowledgeBaseQuery = state.DomainsKnowledgeBaseQuery,
            DomainsKnowledgeBaseDocumentsContent = state.DomainsKnowledgeBaseDocumentsContent.Any()
                ? WorkflowExecutorFormatting.SerializeDocumentation(state.DomainsKnowledgeBaseDocumentsContent)
                : "(No knowledge base results)",
            LanguageOfKnowledgeBase = _workflowConfiguration.LanguageOfKnowledgeBase,
            QmdQueryTypesReference = LoadQmdQueryTypesReference()
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Request Canonicalization Agent", agentInput.ToDictionary());

        var output = await _requestCanonicalizationAgent.ExecuteAsync(agentInput, cancellationToken);

        state.NewCanonicalizedStructuredUserRequest = output.CanonicalizedStructuredUserRequest;
        state.DomainsKnowledgeBaseQuery = output.CanonicalizedDomainsKnowledgeBaseQuery;

        state.AddTokenUsage(RequestCanonicalizationAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Request Canonicalization Agent");

        var notifyDictionary = output.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
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
