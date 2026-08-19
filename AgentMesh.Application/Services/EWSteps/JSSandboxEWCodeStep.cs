using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class JSSandboxEWCodeStep(
        JSSandboxExecutor jsSandboxExecutor,
        GeneratedCodeParameter generatedCodeParameter) : IEWStep
    {
        public string Name => "JS Sandbox";

        public IEnumerable<Type> InputParameterTypes => [typeof(GeneratedCodeParameter)];

        public IEnumerable<Type> OutputParameterTypes => [
            typeof(PipelineResultDataParameter),
            typeof(SandboxExecutionIdParameter),
            typeof(CodeExecutionResultTypeParameter),
            typeof(ExecutionErrorParameter)
            ];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var code = generatedCodeParameter.ValueAs(Values[typeof(GeneratedCodeParameter)]) ?? string.Empty;

            try
            {
                var executionOutput = await jsSandboxExecutor.ExecuteAsync(new CodeSandboxInput
                {
                    Code = code
                });

                return new EWStepExecutionResult
                {
                    OutputMutations = new Dictionary<Type, object?>
                    {
                        { typeof(PipelineResultDataParameter), executionOutput.Result },
                        { typeof(SandboxExecutionIdParameter), executionOutput.ExecutionId },
                        { typeof(CodeExecutionResultTypeParameter), SandboxResultType.Success },
                        { typeof(ExecutionErrorParameter), false }
                    }
                };
            }
            catch (CodeSandboxCallException ex)
            {
                var errorType = ex.ErrorType switch
                {
                    "CodeSyntaxError" => SandboxResultType.SyntaxError,
                    "InvalidRequest" => SandboxResultType.CallError,
                    _ => SandboxResultType.ApplicationError
                };

                return new EWStepExecutionResult
                {
                    OutputMutations = new Dictionary<Type, object?>
                    {
                        { typeof(PipelineResultDataParameter), ex.Message },
                        { typeof(SandboxExecutionIdParameter), string.Empty },
                        { typeof(CodeExecutionResultTypeParameter), errorType },
                        { typeof(ExecutionErrorParameter), true }
                    }
                };
            }
            catch (Exception ex)
            {
                return new EWStepExecutionResult
                {
                    OutputMutations = new Dictionary<Type, object?>
                    {
                        { typeof(PipelineResultDataParameter), ex.Message },
                        { typeof(SandboxExecutionIdParameter), string.Empty },
                        { typeof(CodeExecutionResultTypeParameter), SandboxResultType.ApplicationError },
                        { typeof(ExecutionErrorParameter), true }
                    }
                };
            }
        }
    }
}
