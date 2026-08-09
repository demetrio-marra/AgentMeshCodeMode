using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class JSSandboxEWStep(
        JSSandboxExecutor jsSandboxExecutor,
        GeneratedCodeParameter generatedCodeParameter,
        SandboxResultParameter sandboxResultParameter,
        SandboxExecutionIdParameter sandboxExecutionIdParameter,
        CodeExecutionResultTypeParameter codeExecutionResultTypeParameter,
        ExecutionErrorParameter executionErrorParameter) : IEWStep
    {
        public string Name => "JS Sandbox";

        public bool IsAgentic => false;

        public string? AgentName => null;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var code = generatedCodeParameter.ParameterValue ?? string.Empty;

            try
            {
                var executionOutput = await jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
                {
                    Code = code
                });

                sandboxResultParameter.ParameterValue = executionOutput.Result;
                sandboxExecutionIdParameter.ParameterValue = executionOutput.ExecutionId;
                codeExecutionResultTypeParameter.ParameterValue = SandboxResultType.Success;
                executionErrorParameter.ParameterValue = false;
            }
            catch (CodeSandboxCallException ex)
            {
                sandboxResultParameter.ParameterValue = ex.Message;
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
                sandboxResultParameter.ParameterValue = ex.Message;
                sandboxExecutionIdParameter.ParameterValue = string.Empty;
                codeExecutionResultTypeParameter.ParameterValue = SandboxResultType.ApplicationError;
                executionErrorParameter.ParameterValue = true;
            }

            return new EWStepResultRecord(null, null);
        }
    }
}
