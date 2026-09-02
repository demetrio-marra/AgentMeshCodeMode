using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public sealed class AnalystEWAgenticStep(
        AnalystAgent analystAgent) : IEWAgenticStep
    {
        public string Name => "Analyst";

        public string? AgentName => "Analyst";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(KnowledgeQueryResultParameter),
            typeof(UserIntentParameter),
            typeof(UserPreferencesParameter),
            typeof(UserProvidedDataParameter)
        ];

        public IEnumerable<Type> OutputParameterTypes => [
            typeof(RequestRejectedFlagParameter),
            typeof(AnalystSpecificationParameter),
            typeof(RequestRejectedReasonParameter),
            typeof(AnalystRejectReasonsParameter)
        ];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await analystAgent.ExecuteAsync(Values, cancellationToken);

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(RequestRejectedFlagParameter), !agentOutput.Result.Accepted }, // NOT!
                    { typeof(AnalystSpecificationParameter), agentOutput.Result.Specification },
                    { typeof(RequestRejectedReasonParameter), agentOutput.Result.RejectReason },
                    { typeof(AnalystRejectReasonsParameter), agentOutput.Result.RejectReasons }
                }
            };
        }
    }
}
