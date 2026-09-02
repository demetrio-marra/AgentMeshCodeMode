using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public sealed class KnowledgeQueryBuilderForCoderEWAgenticStep(
        KnowledgeQueryBuilderForCoderAgent knowledgeQueryBuilderForCoderAgent) : IEWAgenticStep
    {
        public string Name => "Knowledge Query Builder For Coder";

        public string? AgentName => "KnowledgeQueryBuilderForCoder";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes =>
        [
            typeof(RequestDateTimeParameter),
            typeof(AnalystSpecificationParameter)
        ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(KnowledgeQueryForCoderParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await knowledgeQueryBuilderForCoderAgent.ExecuteAsync(Values, cancellationToken);

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(KnowledgeQueryForCoderParameter), agentOutput.Result }
                }
            };
        }
    }
}
