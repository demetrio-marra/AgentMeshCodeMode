using AgentMesh.Application.Contracts;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Application.Models.RequestAnalysis;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Models.Workflows.Parameters;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class RequestAnalyzerWorkflowStep(
    ILogger<RequestAnalyzerWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    RequestAnalyzerAgent requestAnalyzerAgent,
    IAgentSelector agentSelector) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Request Analyzer";

    private readonly ILogger<RequestAnalyzerWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly RequestAnalyzerAgent _requestAnalyzerAgent = requestAnalyzerAgent;
    private readonly IAgentSelector _agentSelector = agentSelector;

    public async Task ExecuteRequestAnalyzerAsync(CodeModeWorkflowState state, IEnumerable<ContextMessage> chatHistory)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Request Analyzer Agent...");

        var agentInput = new RequestAnalyzerAgentInput
        {
            ContextMessages = [.. state.InitialContextMessages],
            UserLastRequest = state.UserLastRequest
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Request Analyzer Agent", agentInput.ToDictionary());

        var agentOutput = await _requestAnalyzerAgent.ExecuteAsync(agentInput);

        state.UserRequest = agentOutput;

        state.AddTokenUsage(RequestAnalyzerAgent.AgentName, agentOutput.InputTokenCount, agentOutput.OutputTokenCount, stopwatch.Elapsed, "Request Analyzer Agent");

        var notifyDictionary = agentOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Request Analyzer Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteRequestAnalyzerAsync(stateObject, stateObject.InitialContextMessages);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

public partial class RequestAnalyzerWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => true;

    public override bool IsInputStep => true;

    public override bool IsOutputStep => false;

    public override string? AgentName => RequestAnalyzerAgentConfiguration.AgentName;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(EWParameterNames.UserLastRequest, false),
        new(EWParameterNames.InitialContextMessages, false)
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
                AgentName = RequestAnalyzerAgentConfiguration.AgentName,
                InputTokens = agentOutput.InputTokens,
                OutputTokens = agentOutput.OutputTokens
            }
        };
    }
}
