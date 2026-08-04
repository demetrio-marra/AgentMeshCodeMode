using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Reranker;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Models.Workflows.Parameters;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class RerankerWorkflowStep(
    ILogger<RerankerWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    RerankerAgent rerankerAgent,
    IAgentSelector agentSelector) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Reranker";

    private readonly ILogger<RerankerWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly RerankerAgent _rerankerAgent = rerankerAgent;
    private readonly IAgentSelector _agentSelector = agentSelector;

    public async Task ExecuteRerankerAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var candidates = state.DomainsKnowledgeBaseQueryResults.Results.ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Reranker Agent...");

        var agentInput = new RerankerAgentInput
        {
            StructuredUserRequest = state.CanonicalizedUserRequest ?? state.UserRequest,
            QueryResults = candidates
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Reranker Agent", agentInput.ToDictionary());

        var rerankerOutput = await _rerankerAgent.ExecuteAsync(agentInput, cancellationToken);

        state.DomainsKnowledgeBaseQueryResults = new KnowledgeBaseQueryResult
        {
            Results = rerankerOutput.QueryResults
        };

        state.AddTokenUsage(RerankerAgentConfiguration.AgentName, rerankerOutput.InputTokenCount, rerankerOutput.OutputTokenCount, stopwatch.Elapsed, "Reranker Agent");

        var notifyDictionary = rerankerOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Reranker Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteRerankerAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

public partial class RerankerWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => true;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => RerankerAgentConfiguration.AgentName;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(EWParameterNames.UserIntent, false),
        new(EWParameterNames.KnowledgeBaseQueryResults, false)
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
                AgentName = RerankerAgentConfiguration.AgentName,
                InputTokens = agentOutput.InputTokens,
                OutputTokens = agentOutput.OutputTokens
            }
        };
    }
}
