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

        private readonly JSSandboxExecutor jsSandboxExecutor = jsSandboxExecutor;
        private readonly GeneratedCodeParameter generatedCodeParameter = generatedCodeParameter;
        private readonly SandboxResultParameter sandboxResultParameter = sandboxResultParameter;
        private readonly SandboxExecutionIdParameter sandboxExecutionIdParameter = sandboxExecutionIdParameter;
        private readonly CodeExecutionResultTypeParameter codeExecutionResultTypeParameter = codeExecutionResultTypeParameter;
        private readonly ExecutionErrorParameter executionErrorParameter = executionErrorParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var code = this.generatedCodeParameter.ParameterValue ?? string.Empty;

            try
            {
                var executionOutput = await this.jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
                {
                    Code = code
                });

                this.sandboxResultParameter.ParameterValue = executionOutput.Result;
                this.sandboxExecutionIdParameter.ParameterValue = executionOutput.ExecutionId;
                this.codeExecutionResultTypeParameter.ParameterValue = SandboxResultType.Success;
                this.executionErrorParameter.ParameterValue = false;
            }
            catch (CodeSandboxCallException ex)
            {
                this.sandboxResultParameter.ParameterValue = ex.Message;
                this.sandboxExecutionIdParameter.ParameterValue = string.Empty;

                var errorType = ex.ErrorType switch
                {
                    "CodeSyntaxError" => SandboxResultType.SyntaxError,
                    "InvalidRequest" => SandboxResultType.CallError,
                    _ => SandboxResultType.ApplicationError
                };

                this.codeExecutionResultTypeParameter.ParameterValue = errorType;
                this.executionErrorParameter.ParameterValue = true;
            }
            catch (Exception ex)
            {
                this.sandboxResultParameter.ParameterValue = ex.Message;
                this.sandboxExecutionIdParameter.ParameterValue = string.Empty;
                this.codeExecutionResultTypeParameter.ParameterValue = SandboxResultType.ApplicationError;
                this.executionErrorParameter.ParameterValue = true;
            }

            return new EWStepResultRecord(null, null);
        }
    }
}
