using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Models.CodeFixer;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;
using AgentMesh.Application.Models.Workflows;

namespace AgentMesh.Application.Services.Workflows.Steps;

public class CodeFixerForRuntimeErrorsWorkflowStep(
    ILogger<CodeFixerForRuntimeErrorsWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    CodeFixerAgent codeFixerAgent) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Code Fixer For Runtime Errors";

    private readonly ILogger<CodeFixerForRuntimeErrorsWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly CodeFixerAgent _codeFixerAgent = codeFixerAgent;

    public async Task ExecuteCodeFixerForRuntimeErrorsAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Code Fixer Agent for runtime errors...");

        var agentInput = new CodeFixerAgentInput
        {
            CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty,
            Issues = [state.CodeExecutionAnalysis ?? string.Empty]
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Code Fixer Agent for Runtime Errors", agentInput.ToDictionary());

        var codeFixerOutput = await _codeFixerAgent.ExecuteAsync(agentInput, cancellationToken);
        state.GeneratedCode = codeFixerOutput.FixedCode;
        state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount, stopwatch.Elapsed, "Code Fixer Agent for Runtime Errors");

        var notifyDictionary = codeFixerOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Code Fixer Agent for Runtime Errors", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteCodeFixerForRuntimeErrorsAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

