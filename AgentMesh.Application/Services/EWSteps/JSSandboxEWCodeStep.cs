using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class JSSandboxEWCodeStep(
        JSSandboxExecutor jsSandboxExecutor,
        GeneratedCodeParameter generatedCodeParameter,
        PipelineResultDataParameter pipelineResultDataParameter,
        SandboxExecutionIdParameter sandboxExecutionIdParameter,
        CodeExecutionResultTypeParameter codeExecutionResultTypeParameter,
        ExecutionErrorParameter executionErrorParameter) : IEWCodeStep
    {
        public string Name => "JS Sandbox";

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var code = generatedCodeParameter.ParameterValue ?? string.Empty;

            try
            {
                var executionOutput = await jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
                {
                    Code = code
                });

                pipelineResultDataParameter.ParameterValue = executionOutput.Result;
                sandboxExecutionIdParameter.ParameterValue = executionOutput.ExecutionId;
                codeExecutionResultTypeParameter.ParameterValue = SandboxResultType.Success;
                executionErrorParameter.ParameterValue = false;
            }
            catch (CodeSandboxCallException ex)
            {
                pipelineResultDataParameter.ParameterValue = ex.Message;
                sandboxExecutionIdParameter.ParameterValue = string.Empty;

                var errorType = ex.ErrorType switch
                {
                    "CodeSyntaxError" => SandboxResultType.SyntaxError,
                    "InvalidRequest" => SandboxResultType.CallError,
                    _ => SandboxResultType.ApplicationError
                };

                codeExecutionResultTypeParameter.ParameterValue = errorType;
                executionErrorParameter.ParameterValue = true;
            }
            catch (Exception ex)
            {
                pipelineResultDataParameter.ParameterValue = ex.Message;
                sandboxExecutionIdParameter.ParameterValue = string.Empty;
                codeExecutionResultTypeParameter.ParameterValue = SandboxResultType.ApplicationError;
                executionErrorParameter.ParameterValue = true;
            }
        }
    }
}
