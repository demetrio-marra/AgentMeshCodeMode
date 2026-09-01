using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class CoderEWAgenticStep(
        CoderAgent coderAgent) : IEWAgenticStep
    {
        public string Name => "Coder";

        public string? AgentName => "Coder";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(AnalystSpecificationParameter),
            typeof(KnowledgeContentForCoderParameter)
        ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(GeneratedCodeParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await coderAgent.ExecuteAsync(Values, cancellationToken);

            return new EWAgenticStepExecutionResult
            {
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(GeneratedCodeParameter), agentOutput.Result }
                },
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount
            };
        }
    }
}
