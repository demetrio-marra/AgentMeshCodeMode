using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.CodeExecutionFailuresDetector;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Models.Workflows.Parameters;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class CodeExecutionFailuresDetectorWorkflowStep(
    ILogger<CodeExecutionFailuresDetectorWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    JavascriptCodeExecutionFailuresDetectorAgent codeExecutionFailuresDetectorAgent,
    IAgentSelector agentSelector) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Code Execution Failures Detector";

    private readonly ILogger<CodeExecutionFailuresDetectorWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly JavascriptCodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent = codeExecutionFailuresDetectorAgent;
    private readonly IAgentSelector _agentSelector = agentSelector;

    public async Task ExecuteCodeExecutionFailuresDetectorAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Code Execution Failures Detector Agent...");

        var agentInput = new CodeExecutionFailuresDetectorAgentInput
        {
            CodeWithLineNumbers = state.LastCodeWithLineNumbers ?? string.Empty,
            ExecutionResult = state.SandboxResult ?? string.Empty
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Code Execution Failures Detector Agent", agentInput.ToDictionary());

        var detectorOutput = await _codeExecutionFailuresDetectorAgent.ExecuteAsync(agentInput, cancellationToken);
        state.CodeExecutionFailuresDetectorIterationCount++;
        state.CodeExecutionAnalysis = detectorOutput.Analysis;
        state.AddTokenUsage(CodeExecutionFailuresDetectorAgentConfiguration.AgentName, detectorOutput.InputTokenCount, detectorOutput.OutputTokenCount, stopwatch.Elapsed, "Code Execution Failures Detector Agent");

        var notifyDictionary = detectorOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Code Execution Failures Detector Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteCodeExecutionFailuresDetectorAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

public partial class CodeExecutionFailuresDetectorWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => true;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => CodeExecutionFailuresDetectorAgentConfiguration.AgentName;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(EWParameterNames.LastCodeWithLineNumbers, false),
        new(EWParameterNames.SandboxResult, false)
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
                AgentName = CodeExecutionFailuresDetectorAgentConfiguration.AgentName,
                InputTokens = agentOutput.InputTokens,
                OutputTokens = agentOutput.OutputTokens
            }
        };
    }
}

