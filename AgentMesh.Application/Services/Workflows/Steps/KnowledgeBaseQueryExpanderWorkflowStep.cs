using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Models.KnowledgeBaseQueryExpander;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Application.Models.Workflows;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class KnowledgeBaseQueryExpanderWorkflowStep(
    ILogger<KnowledgeBaseQueryExpanderWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    KnowledgeBaseQueryExpanderAgent knowledgeBaseQueryExpanderAgent,
    CodeModeWorkflowConfiguration workflowConfiguration,
    IAgentSelector agentSelector) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string QmdQueryTypesFileName = "QMDQueryTypes.md";
    private const string WorkflowStepDisplayName = "Knowledge Base Query Expander";

    private readonly ILogger<KnowledgeBaseQueryExpanderWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly KnowledgeBaseQueryExpanderAgent _knowledgeBaseQueryExpanderAgent = knowledgeBaseQueryExpanderAgent;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;
    private readonly IAgentSelector _agentSelector = agentSelector;

    public async Task ExecuteKnowledgeBaseQueryExpanderAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var sr = state.UserRequest!;

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Knowledge Base Query Expander Agent...");

        var agentInput = new KnowledgeBaseQueryExpanderAgentInput
        {
            StructuredUserRequest = sr,
            GenerateHydeQueries = sr.IntentCategory == AgentMesh.Models.RequestAnalysis.UserIntentCategory.Documentation,
            DocumentationQueriesGenerationReference = LoadDocumentationQueriesGenerationReference()
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Knowledge Base Query Expander Agent", agentInput.ToDictionary());

        var queryExpanderOutput = await _knowledgeBaseQueryExpanderAgent.ExecuteAsync(agentInput, cancellationToken);

        // filter also on return
        var searchQueries = queryExpanderOutput.SearchQueries.ToList();
        if (sr.IntentCategory != AgentMesh.Models.RequestAnalysis.UserIntentCategory.Documentation)
        {
            searchQueries = searchQueries.Where(q => q.SearchType != AgentMesh.Models.KnowledgeBase.KnowledgeBaseQuerySearchType.HypotheticalDocument).ToList();
        }

        state.DomainsKnowledgeBaseQuery = searchQueries;
        state.AddTokenUsage(KnowledgeBaseQueryExpanderAgentConfiguration.AgentName, queryExpanderOutput.InputTokenCount, queryExpanderOutput.OutputTokenCount, stopwatch.Elapsed, "Knowledge Base Query Expander Agent");

        var notifyDictionary = queryExpanderOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Knowledge Base Query Expander Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteKnowledgeBaseQueryExpanderAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }

    private string? LoadDocumentationQueriesGenerationReference()
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

public partial class KnowledgeBaseQueryExpanderWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => true;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => KnowledgeBaseQueryExpanderAgentConfiguration.AgentName;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(CodeModeWorkflowParametersFactory.UserIntentParameterName, false),
        new(CodeModeWorkflowParametersFactory.IntentCategoryParameterName, false),
        new(CodeModeWorkflowParametersFactory.UserRequestedActionsParameterName, false),
        new(CodeModeWorkflowParametersFactory.UserProvidedDataParameterName, false)
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
                AgentName = KnowledgeBaseQueryExpanderAgentConfiguration.AgentName,
                InputTokens = agentOutput.InputTokens,
                OutputTokens = agentOutput.OutputTokens
            }
        };
    }
}
