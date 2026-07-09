using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.CodeExecutionFailuresDetector;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Executors;

public class CodeExecutionFailuresDetectorWorkflowExecutor(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    ICodeExecutionFailuresDetectorAgent codeExecutionFailuresDetectorAgent)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly ICodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent = codeExecutionFailuresDetectorAgent;

    public async Task<string> ExecuteCodeExecutionFailuresDetectorAsync(CodeModeWorkflowState state, int iteration)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Code Execution Failures Detector Agent... Iteration {Iteration}", iteration);
        await _workflowProgressNotifier.NotifyWorkflowStepStart($"Code Execution Failures Detector Agent (Iteration {iteration})", new Dictionary<string, string>
        {
            { "CodeWithLineNumbers", state.LastCodeWithLineNumbers ?? "(No code available)" },
            { "ExecutionResult", state.SandboxResult ?? "(No execution result)" }
        });

        var detectorOutput = await _codeExecutionFailuresDetectorAgent.ExecuteAsync(new CodeExecutionFailuresDetectorAgentInput
        {
            CodeWithLineNumbers = state.LastCodeWithLineNumbers ?? string.Empty,
            ExecutionResult = state.SandboxResult ?? string.Empty
        });
        state.CodeExecutionFailuresDetectorIterationCount++;
        state.AddTokenUsage(CodeExecutionFailuresDetectorAgentConfiguration.AgentName, detectorOutput.InputTokenCount, detectorOutput.OutputTokenCount, stopwatch.Elapsed, $"Code Execution Failures Detector Agent (Iteration {iteration})");
        var notifyDictionary = new Dictionary<string, string>
        {
            { "Analysis", detectorOutput.Analysis },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd($"Code Execution Failures Detector Agent (Iteration {iteration})", notifyDictionary);

        return detectorOutput.Analysis;
    }
}

