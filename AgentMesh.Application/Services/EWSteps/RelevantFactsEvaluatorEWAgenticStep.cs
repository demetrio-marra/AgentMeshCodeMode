using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RelevantFactsEvaluatorEWAgenticStep(
        RelevantFactsEvaluatorAgent agent) : IEWAgenticStep
    {
        public string Name => "Relevant Facts Evaluator";

        public string? AgentName => "RelevantFactsEvaluator";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(MessagesToSummarizeParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(RelevantMessagesToSaveInAgentMemoryParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await agent.ExecuteAsync(Values, cancellationToken);

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(RelevantMessagesToSaveInAgentMemoryParameter), agentOutput.Result }
                }
            };
        }
    }
}
