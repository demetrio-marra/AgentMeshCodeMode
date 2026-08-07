using AgentMesh.Application.Models.CodeExecutionFailuresDetector;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class CodeExecutionFailuresDetectorEWStep(
        JavascriptCodeExecutionFailuresDetectorAgent codeExecutionFailuresDetectorAgent,
        LastCodeWithLineNumbersParameter lastCodeWithLineNumbersParameter,
        SandboxResultParameter sandboxResultParameter,
        CodeExecutionAnalysisParameter codeExecutionAnalysisParameter,
        CodeExecutionFailuresDetectorIterationCountParameter codeExecutionFailuresDetectorIterationCountParameter) : IEWStep
    {
        public string Name => "Code Execution Failures Detector";

        public bool IsAgentic => true;

        public string? AgentName => CodeExecutionFailuresDetectorAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly JavascriptCodeExecutionFailuresDetectorAgent _codeExecutionFailuresDetectorAgent = codeExecutionFailuresDetectorAgent;
        private readonly LastCodeWithLineNumbersParameter _lastCodeWithLineNumbersParameter = lastCodeWithLineNumbersParameter;
        private readonly SandboxResultParameter _sandboxResultParameter = sandboxResultParameter;
        private readonly CodeExecutionAnalysisParameter _codeExecutionAnalysisParameter = codeExecutionAnalysisParameter;
        private readonly CodeExecutionFailuresDetectorIterationCountParameter _codeExecutionFailuresDetectorIterationCountParameter = codeExecutionFailuresDetectorIterationCountParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentInput = new CodeExecutionFailuresDetectorAgentInput
            {
                CodeWithLineNumbers = _lastCodeWithLineNumbersParameter.ParameterValue ?? string.Empty,
                ExecutionResult = _sandboxResultParameter.ParameterValue ?? string.Empty
            };

            var agentOutput = await _codeExecutionFailuresDetectorAgent.ExecuteAsync(agentInput, cancellationToken);

            _codeExecutionAnalysisParameter.ParameterValue = agentOutput.Analysis;

            var currentIterationCount = _codeExecutionFailuresDetectorIterationCountParameter.ParameterValue ?? 0;
            _codeExecutionFailuresDetectorIterationCountParameter.ParameterValue = currentIterationCount + 1;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
