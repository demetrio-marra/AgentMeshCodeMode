using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class JSSandboxEWStep(
        JSSandboxExecutor jsSandboxExecutor,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "JS Sandbox";

        public bool IsAgentic => false;

        public string? AgentName => null;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.GeneratedCode
        ];

        private readonly JSSandboxExecutor _jsSandboxExecutor = jsSandboxExecutor;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var generatedCodeParameter = inputParameters.Single(p => p.Name == EWParameterNames.GeneratedCode);
            if (generatedCodeParameter is not GeneratedCodeParameter typedGeneratedCode)
                throw new InvalidOperationException($"Parameter {EWParameterNames.GeneratedCode} is not of type GeneratedCodeParameter");

            var code = typedGeneratedCode.ParameterValue ?? string.Empty;

            try
            {
                var executionOutput = await _jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
                {
                    Code = code
                });

                _ewParametersProvider.UpdateParameterValue(EWParameterNames.SandboxResult, executionOutput.Result);
                _ewParametersProvider.UpdateParameterValue(EWParameterNames.SandboxExecutionId, executionOutput.ExecutionId);
                _ewParametersProvider.UpdateParameterValue(EWParameterNames.CodeExecutionResultType, SandboxResultType.Success);
                _ewParametersProvider.UpdateParameterValue(EWParameterNames.ExecutionError, false);
            }
            catch (CodeSandboxCallException ex)
            {
                _ewParametersProvider.UpdateParameterValue(EWParameterNames.SandboxResult, ex.Message);
                _ewParametersProvider.UpdateParameterValue(EWParameterNames.SandboxExecutionId, string.Empty);

                var errorType = ex.ErrorType switch
                {
                    "CodeSyntaxError" => SandboxResultType.SyntaxError,
                    "InvalidRequest" => SandboxResultType.CallError,
                    _ => SandboxResultType.ApplicationError
                };

                _ewParametersProvider.UpdateParameterValue(EWParameterNames.CodeExecutionResultType, errorType);
                _ewParametersProvider.UpdateParameterValue(EWParameterNames.ExecutionError, true);
            }
            catch (Exception ex)
            {
                _ewParametersProvider.UpdateParameterValue(EWParameterNames.SandboxResult, ex.Message);
                _ewParametersProvider.UpdateParameterValue(EWParameterNames.SandboxExecutionId, string.Empty);
                _ewParametersProvider.UpdateParameterValue(EWParameterNames.CodeExecutionResultType, SandboxResultType.ApplicationError);
                _ewParametersProvider.UpdateParameterValue(EWParameterNames.ExecutionError, true);
            }

            return new EWStepResultRecord(null, null);
        }
    }
}
