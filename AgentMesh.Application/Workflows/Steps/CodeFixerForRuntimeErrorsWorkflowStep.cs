using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.CodeFixer;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;

namespace AgentMesh.Application.Workflows.Steps;

public class CodeFixerForRuntimeErrorsWorkflowStep(
    ILogger<CodeFixerForRuntimeErrorsWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    CodeFixerAgent codeFixerAgent) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Code Fixer For Runtime Errors";

    private readonly ILogger<CodeFixerForRuntimeErrorsWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly CodeFixerAgent _codeFixerAgent = codeFixerAgent;

    public async Task ExecuteCodeFixerForRuntimeErrorsAsync(CodeModeWorkflowState state, string analysis, int iteration)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Code Fixer Agent for runtime errors... Iteration {Iteration}", iteration);

        var agentInput = new CodeFixerAgentInput
        {
            CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty,
            Issues = [analysis]
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart($"Code Fixer Agent for Runtime Errors (Iteration {iteration})", agentInput.ToDictionary());

        var codeFixerOutput = await _codeFixerAgent.ExecuteAsync(agentInput);
        state.GeneratedCode = codeFixerOutput.FixedCode;
        state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount, stopwatch.Elapsed, $"Code Fixer Agent for Runtime Errors (Iteration {iteration})");

        var notifyDictionary = codeFixerOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd($"Code Fixer Agent for Runtime Errors (Iteration {iteration})", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var analysis = stateObject.SandboxResult ?? string.Empty;
        await ExecuteCodeFixerForRuntimeErrorsAsync(stateObject, analysis, 1);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

