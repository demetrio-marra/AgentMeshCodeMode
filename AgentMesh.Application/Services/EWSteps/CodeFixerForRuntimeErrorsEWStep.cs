using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.CodeFixer;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class CodeFixerForRuntimeErrorsEWStep(
        CodeFixerAgent codeFixerAgent,
        LastCodeWithLineNumbersParameter lastCodeWithLineNumbersParameter,
        CodeExecutionAnalysisParameter codeExecutionAnalysisParameter,
        GeneratedCodeParameter generatedCodeParameter) : IEWStep
    {
        public string Name => "Code Fixer For Runtime Errors";

        public bool IsAgentic => true;

        public string? AgentName => CodeFixerAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly CodeFixerAgent _codeFixerAgent = codeFixerAgent;
        private readonly LastCodeWithLineNumbersParameter _lastCodeWithLineNumbersParameter = lastCodeWithLineNumbersParameter;
        private readonly CodeExecutionAnalysisParameter _codeExecutionAnalysisParameter = codeExecutionAnalysisParameter;
        private readonly GeneratedCodeParameter _generatedCodeParameter = generatedCodeParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentInput = new CodeFixerAgentInput
            {
                CodeToFix = _lastCodeWithLineNumbersParameter.ParameterValue ?? string.Empty,
                Issues = [_codeExecutionAnalysisParameter.ParameterValue ?? string.Empty]
            };

            var agentOutput = await _codeFixerAgent.ExecuteAsync(agentInput, cancellationToken);

            _generatedCodeParameter.ParameterValue = agentOutput.FixedCode;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
