using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.CodeExecutionFailuresDetector;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;

namespace AgentMesh.Application.Workflows.Steps;

public class CodeExecutionFailuresDetectorWorkflowStep(
    ILogger<CodeExecutionFailuresDetectorWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    JavascriptCodeExecutionFailuresDetectorAgent codeExecutionFailuresDetectorAgent) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Code Execution Failures Detector";

    private readonly ILogger<CodeExecutionFailuresDetectorWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly JavascriptCodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent = codeExecutionFailuresDetectorAgent;

    public async Task<string> ExecuteCodeExecutionFailuresDetectorAsync(CodeModeWorkflowState state, int iteration)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Code Execution Failures Detector Agent... Iteration {Iteration}", iteration);

        var agentInput = new CodeExecutionFailuresDetectorAgentInput
        {
            CodeWithLineNumbers = state.LastCodeWithLineNumbers ?? string.Empty,
            ExecutionResult = state.SandboxResult ?? string.Empty
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart($"Code Execution Failures Detector Agent (Iteration {iteration})", agentInput.ToDictionary());

        var detectorOutput = await _codeExecutionFailuresDetectorAgent.ExecuteAsync(agentInput);
        state.CodeExecutionFailuresDetectorIterationCount++;
        state.AddTokenUsage(CodeExecutionFailuresDetectorAgentConfiguration.AgentName, detectorOutput.InputTokenCount, detectorOutput.OutputTokenCount, stopwatch.Elapsed, $"Code Execution Failures Detector Agent (Iteration {iteration})");

        var notifyDictionary = detectorOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd($"Code Execution Failures Detector Agent (Iteration {iteration})", notifyDictionary);

        return detectorOutput.Analysis;
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteCodeExecutionFailuresDetectorAsync(stateObject, 1);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

