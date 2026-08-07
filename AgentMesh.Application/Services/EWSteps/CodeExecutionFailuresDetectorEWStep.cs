using AgentMesh.Application.Models.CodeExecutionFailuresDetector;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class CodeExecutionFailuresDetectorEWStep(
        JavascriptCodeExecutionFailuresDetectorAgent codeExecutionFailuresDetectorAgent,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Code Execution Failures Detector";

        public bool IsAgentic => true;

        public string? AgentName => CodeExecutionFailuresDetectorAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.LastCodeWithLineNumbers,
            EWParameterNames.SandboxResult
        ];

        private readonly JavascriptCodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent = codeExecutionFailuresDetectorAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var codeParameter = inputParameters.Single(p => p.Name == EWParameterNames.LastCodeWithLineNumbers);
            if (codeParameter is not LastCodeWithLineNumbersParameter typedCode)
                throw new InvalidOperationException($"Parameter {EWParameterNames.LastCodeWithLineNumbers} is not of type LastCodeWithLineNumbersParameter");

            var sandboxResultParameter = inputParameters.Single(p => p.Name == EWParameterNames.SandboxResult);
            if (sandboxResultParameter is not SandboxResultParameter typedSandboxResult)
                throw new InvalidOperationException($"Parameter {EWParameterNames.SandboxResult} is not of type SandboxResultParameter");

            var agentInput = new CodeExecutionFailuresDetectorAgentInput
            {
                CodeWithLineNumbers = typedCode.ParameterValue ?? string.Empty,
                ExecutionResult = typedSandboxResult.ParameterValue ?? string.Empty
            };

            var agentOutput = await _codeExecutionFailuresDetectorAgent.ExecuteAsync(agentInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.CodeExecutionAnalysis, agentOutput.Analysis);

            var currentIterationCount = (_ewParametersProvider.GetParameters([EWParameterNames.CodeExecutionFailuresDetectorIterationCount])
                .FirstOrDefault() as CodeExecutionFailuresDetectorIterationCountParameter)?.ParameterValue ?? 0;
            _ewParametersProvider.UpdateParameterValue<int?>(EWParameterNames.CodeExecutionFailuresDetectorIterationCount, currentIterationCount + 1);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
