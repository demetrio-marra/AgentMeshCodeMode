using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Models.CodeExecutionFailuresDetector;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;
using AgentMesh.Application.Models.Workflows;

namespace AgentMesh.Application.Services.Workflows.Steps;

public class CodeExecutionFailuresDetectorWorkflowStep(
    ILogger<CodeExecutionFailuresDetectorWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    JavascriptCodeExecutionFailuresDetectorAgent codeExecutionFailuresDetectorAgent) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Code Execution Failures Detector";

    private readonly ILogger<CodeExecutionFailuresDetectorWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly JavascriptCodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent = codeExecutionFailuresDetectorAgent;

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

