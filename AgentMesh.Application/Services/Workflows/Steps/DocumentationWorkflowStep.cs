using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Models.Documentation;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;
using AgentMesh.Application.Models.Workflows;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class DocumentationWorkflowStep(
    ILogger<DocumentationWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    DocumentationAgent documentationAgent,
    IAgentSelector agentSelector) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Documentation";

    private readonly ILogger<DocumentationWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly DocumentationAgent _documentationAgent = documentationAgent;
    private readonly IAgentSelector _agentSelector = agentSelector;

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

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteDocumentationAgentAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

public partial class DocumentationWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => true;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => DocumentationAgentConfiguration.AgentName;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(CodeModeWorkflowParametersFactory.UserIntentParameterName, false),
        new(CodeModeWorkflowParametersFactory.PastMemoriesQueryResultsParameterName, false),
        new(CodeModeWorkflowParametersFactory.DomainsKnowledgeBaseDocumentsContentParameterName, true),
        new(CodeModeWorkflowParametersFactory.LanguageOfTheUserParameterName, false)
    ];

    public override async Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default)
    {
        var agentInput = ToAgentInputParameters(inputParameters);

        var agent = _agentSelector.GetAgent(AgentName!);
        var agentOutput = await agent.ExecuteAsync(agentInput, cancellationToken);

        return new WorkflowStepResultRecord
        {
            OutputParameters = agentOutput.OutputParameters.ToDictionary(p => p.Name, p => p.Value),
            AgentTokenUsageEntry = new AgentTokenUsageEntry
            {
                AgentName = DocumentationAgentConfiguration.AgentName,
                InputTokens = agentOutput.InputTokens,
                OutputTokens = agentOutput.OutputTokens
            }
        };
    }
}

