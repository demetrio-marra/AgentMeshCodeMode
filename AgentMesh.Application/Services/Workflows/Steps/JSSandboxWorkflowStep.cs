using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Services;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Application.Models.Workflows;

namespace AgentMesh.Application.Services.Workflows.Steps;

public class JSSandboxWorkflowStep(
    ILogger<JSSandboxWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    JSSandboxExecutor jsSandboxExecutor) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "JS Sandbox";

    private readonly ILogger<JSSandboxWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly JSSandboxExecutor _jsSandboxExecutor = jsSandboxExecutor;

    public async Task<bool> ExecuteJSSandboxAsync(CodeModeWorkflowState state, bool isReexecution)
    {
        var stopwatch = Stopwatch.StartNew();
        var stepName = isReexecution ? "JS Sandbox Executor (Re-execution)" : "JS Sandbox Executor";
        var iterationMessage = isReexecution ? "again after code revision" : "";

        bool sandBoxError = false;
        try
        {
            _logger.LogDebug("Running JS Sandbox Executor {IterationMessage}", iterationMessage);
            await _workflowProgressNotifier.NotifyWorkflowStepStart(stepName, new Dictionary<string, string>
            {
                { "Code", state.GeneratedCode ?? "(No generated code)" }
            });

            var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
            {
                Code = state.GeneratedCode ?? string.Empty
            });
            state.SandboxResult = executionOutput.Result;
            state.SandboxExecutionId = executionOutput.ExecutionId;
            state.CodeExecutionResultType = SandboxResultType.Success;
            var notifyDictionary = new Dictionary<string, string>
            {
                { "ExecutionId", state.SandboxExecutionId },
                { "Result", state.SandboxResult },
                { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
        }
        catch (CodeSandboxCallException ex)
        {
            state.SandboxResult = ex.Message;
            state.CodeExecutionResultType = ex.ErrorType switch
            {
                "CodeSyntaxError" => SandboxResultType.SyntaxError,
                "InvalidRequest" => SandboxResultType.CallError,
                _ => SandboxResultType.ApplicationError
            };
            sandBoxError = true;
            var notifyDictionary = new Dictionary<string, string>
            {
                { "Error", state.SandboxResult },
                { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
        }
        catch (Exception ex)
        {
            state.SandboxResult = ex.Message;
            state.CodeExecutionResultType = SandboxResultType.ApplicationError;
            sandBoxError = true;
            var notifyDictionary = new Dictionary<string, string>
            {
                { "Error", state.SandboxResult },
                { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
            };
            await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
        }

        state.AddStepUsage(stepName, stopwatch.Elapsed, false);

        return sandBoxError;
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteJSSandboxAsync(stateObject, false);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

