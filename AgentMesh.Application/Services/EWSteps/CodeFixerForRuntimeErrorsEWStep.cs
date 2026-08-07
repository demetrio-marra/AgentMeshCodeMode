using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.CodeFixer;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class CodeFixerForRuntimeErrorsEWStep(
        CodeFixerAgent codeFixerAgent,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Code Fixer For Runtime Errors";

        public bool IsAgentic => true;

        public string? AgentName => CodeFixerAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.LastCodeWithLineNumbers,
            EWParameterNames.CodeExecutionAnalysis
        ];

        private readonly CodeFixerAgent _codeFixerAgent = codeFixerAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var codeParameter = inputParameters.Single(p => p.Name == EWParameterNames.LastCodeWithLineNumbers);
            if (codeParameter is not LastCodeWithLineNumbersParameter typedCode)
                throw new InvalidOperationException($"Parameter {EWParameterNames.LastCodeWithLineNumbers} is not of type LastCodeWithLineNumbersParameter");

            var analysisParameter = inputParameters.Single(p => p.Name == EWParameterNames.CodeExecutionAnalysis);
            if (analysisParameter is not CodeExecutionAnalysisParameter typedAnalysis)
                throw new InvalidOperationException($"Parameter {EWParameterNames.CodeExecutionAnalysis} is not of type CodeExecutionAnalysisParameter");

            var agentInput = new CodeFixerAgentInput
            {
                CodeToFix = typedCode.ParameterValue ?? string.Empty,
                Issues = [typedAnalysis.ParameterValue ?? string.Empty]
            };

            var agentOutput = await _codeFixerAgent.ExecuteAsync(agentInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.GeneratedCode, agentOutput.FixedCode);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
