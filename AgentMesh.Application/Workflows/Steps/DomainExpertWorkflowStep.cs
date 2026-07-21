using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.DomainExpert;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class DomainExpertWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IDomainExpertAgent domainExpertAgent)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IDomainExpertAgent _domainExpertAgent = domainExpertAgent;

    public async Task ExecuteDomainExpertAgentAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Domain Expert Agent...");

        var dataToComment = state.SandboxResult ?? string.Empty;
        var serializedDocumentation = WorkflowExecutorFormatting.SerializeDocumentation(state.DomainsKnowledgeBaseDocumentsContent);

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Domain Expert Agent", new Dictionary<string, string>
        {
            { "Intent", state.Intent },
            { "UserRequestedActions", state.UserRequestedActions.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserRequestedActions) : "(No actions)" },
            { "UserProvidedData", state.UserProvidedData.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserProvidedData) : "(No data)" },
            { "UserPreferences", state.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(state.UserPreferences) : "(No user preferences)" },
            { "MemoriesFromAgentMemoryService", state.PastMemoriesQueryResults.Any() ? WorkflowExecutorFormatting.ToBulletList(state.PastMemoriesQueryResults.Select(m => m.Memory)) : "(No memories)" },
            { "DomainsKnowledgeBaseDocumentsContent", state.DomainsKnowledgeBaseDocumentsContent.Any() ? WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseDocumentsContent.Select(d => d.File)) : "(No documents)" },
            { "DataToComment", string.IsNullOrWhiteSpace(dataToComment) ? "(No sandbox result)" : dataToComment }
        });

        var output = await _domainExpertAgent.ExecuteAsync(new DomainExpertAgentInput
        {
            Intent = state.Intent,
            SupportingIntentInformation = state.UserRequestedActions,
            Entities = new Dictionary<string, IEnumerable<string>>(),
            UserPreferences = state.UserPreferences,
            AgentMemories = state.PastMemoriesQueryResults.Select(m => m.Memory),
            KnowledgeBaseDocumentsContent = serializedDocumentation,
            DataToComment = dataToComment
        }, cancellationToken);

        state.DomainExpertOutput = output.DomainExpertComment;
        state.AddTokenUsage(DomainExpertAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Domain Expert Agent");

        var notifyDictionary = new Dictionary<string, string>
        {
            { "Content", state.DomainExpertOutput },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Domain Expert Agent", notifyDictionary);
    }
}

