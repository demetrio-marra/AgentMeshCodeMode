using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Models.Workflows.Parameters;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class JSSandboxWorkflowStep(
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

public partial class JSSandboxWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => false;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => null;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(EWParameterNames.GeneratedCode, false)
    ];

    public override async Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default)
    {
        var codeParameter = inputParameters.FirstOrDefault(p => p.Name == EWParameterNames.GeneratedCode);
        var code = codeParameter.RawValue ?? string.Empty;

        string? sandboxResult = null;
        string? sandboxExecutionId = null;
        string? codeExecutionResultType = null;

        try
        {
            var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
            {
                Code = code
            });
            sandboxResult = executionOutput.Result;
            sandboxExecutionId = executionOutput.ExecutionId;
            codeExecutionResultType = SandboxResultType.Success.ToString();
        }
        catch (CodeSandboxCallException ex)
        {
            sandboxResult = ex.Message;
            codeExecutionResultType = ex.ErrorType switch
            {
                "CodeSyntaxError" => SandboxResultType.SyntaxError.ToString(),
                "InvalidRequest" => SandboxResultType.CallError.ToString(),
                _ => SandboxResultType.ApplicationError.ToString()
            };
        }
        catch (Exception ex)
        {
            sandboxResult = ex.Message;
            codeExecutionResultType = SandboxResultType.ApplicationError.ToString();
        }

        return new WorkflowStepResultRecord
        {
            OutputParameters = new Dictionary<string, string?>
            {
                { EWParameterNames.SandboxResult, sandboxResult },
                { EWParameterNames.SandboxExecutionId, sandboxExecutionId },
                { EWParameterNames.CodeExecutionResultType, codeExecutionResultType }
            }
        };
    }
}

