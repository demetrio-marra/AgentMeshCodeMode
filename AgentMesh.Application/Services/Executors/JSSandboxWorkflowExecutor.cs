using AgentMesh.Services;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.CodeSandbox;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Executors;

public class JSSandboxWorkflowExecutor(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IJSSandboxExecutor jsSandboxExecutor)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IJSSandboxExecutor _jsSandboxExecutor = jsSandboxExecutor;

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
}

