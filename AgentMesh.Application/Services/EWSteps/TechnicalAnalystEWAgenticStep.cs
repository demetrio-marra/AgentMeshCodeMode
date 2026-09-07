using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class TechnicalAnalystEWAgenticStep(
        TechnicalAnalystAgent technicalAnalystAgent) : IEWAgenticStep
    {
        public string Name => "Technical Analyst";

        public string? AgentName => "TechnicalAnalyst";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(BusinessRequirementsParameter),
            typeof(KnowledgeContentForCoderParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [
            typeof(RequestRejectedFlagParameter),
            typeof(RequestRejectedReasonParameter)
            ];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await technicalAnalystAgent.ExecuteAsync(Values, cancellationToken);

            var outputMutations = new Dictionary<Type, object?>
            {
                { typeof(RequestRejectedFlagParameter), agentOutput.Result.RequestRejected },
                { typeof(RequestRejectedReasonParameter), agentOutput.Result.RequestRejectionReason }
            };

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = outputMutations
            };
        }
    }
}
