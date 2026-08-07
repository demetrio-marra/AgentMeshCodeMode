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

        private readonly JSSandboxExecutor _jsSandboxExecutor = jsSandboxExecutor;
        private readonly GeneratedCodeParameter _generatedCodeParameter = generatedCodeParameter;
        private readonly SandboxResultParameter _sandboxResultParameter = sandboxResultParameter;
        private readonly SandboxExecutionIdParameter _sandboxExecutionIdParameter = sandboxExecutionIdParameter;
        private readonly CodeExecutionResultTypeParameter _codeExecutionResultTypeParameter = codeExecutionResultTypeParameter;
        private readonly ExecutionErrorParameter _executionErrorParameter = executionErrorParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var code = _generatedCodeParameter.ParameterValue ?? string.Empty;

            try
            {
                var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
                {
                    Code = code
                });

                _sandboxResultParameter.ParameterValue = executionOutput.Result;
                _sandboxExecutionIdParameter.ParameterValue = executionOutput.ExecutionId;
                _codeExecutionResultTypeParameter.ParameterValue = SandboxResultType.Success;
                _executionErrorParameter.ParameterValue = false;
            }
            catch (CodeSandboxCallException ex)
            {
                _sandboxResultParameter.ParameterValue = ex.Message;
                _sandboxExecutionIdParameter.ParameterValue = string.Empty;

                var errorType = ex.ErrorType switch
                {
                    "CodeSyntaxError" => SandboxResultType.SyntaxError,
                    "InvalidRequest" => SandboxResultType.CallError,
                    _ => SandboxResultType.ApplicationError
                };

                _codeExecutionResultTypeParameter.ParameterValue = errorType;
                _executionErrorParameter.ParameterValue = true;
            }
            catch (Exception ex)
            {
                _sandboxResultParameter.ParameterValue = ex.Message;
                _sandboxExecutionIdParameter.ParameterValue = string.Empty;
                _codeExecutionResultTypeParameter.ParameterValue = SandboxResultType.ApplicationError;
                _executionErrorParameter.ParameterValue = true;
            }

            return new EWStepResultRecord(null, null);
        }
    }
}
