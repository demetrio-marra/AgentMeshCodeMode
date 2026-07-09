using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.CodeFixer;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Executors;

public class CodeFixerForRuntimeErrorsWorkflowExecutor(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    ICodeFixerAgent codeFixerAgent)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly ICodeFixerAgent _codeFixerAgent = codeFixerAgent;

    public async Task ExecuteCodeFixerForRuntimeErrorsAsync(CodeModeWorkflowState state, string analysis, int iteration)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Code Fixer Agent for runtime errors... Iteration {Iteration}", iteration);
        await _workflowProgressNotifier.NotifyWorkflowStepStart($"Code Fixer Agent for Runtime Errors (Iteration {iteration})", new Dictionary<string, string>
        {
            { "CodeToFix", state.LastCodeWithLineNumbers ?? "(No code available)" },
            { "IssuesCount", "1" }
        });

        var codeFixerOutput = await _codeFixerAgent.ExecuteAsync(new CodeFixerAgentInput
        {
            CodeToFix = state.LastCodeWithLineNumbers ?? string.Empty,
            Issues = [analysis]
        });
        state.GeneratedCode = codeFixerOutput.FixedCode;
        state.AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount, stopwatch.Elapsed, $"Code Fixer Agent for Runtime Errors (Iteration {iteration})");
        var notifyDictionary = new Dictionary<string, string>
        {
            { "FixedCode", state.GeneratedCode },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd($"Code Fixer Agent for Runtime Errors (Iteration {iteration})", notifyDictionary);
    }
}

