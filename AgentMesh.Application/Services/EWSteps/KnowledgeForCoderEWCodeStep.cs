using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class KnowledgeForCoderEWCodeStep(
        IKnowledgeService knowledgeService,
        KnowledgeQueryForCoderParameter knowledgeQueryParameter) : IEWStep
    {
        public string Name => "KnowledgeForCoder Service Search";

        public IEnumerable<Type> InputParameterTypes => [typeof(KnowledgeQueryForCoderParameter)];
        public IEnumerable<Type> OutputParameterTypes => [typeof(KnowledgeQueryForCoderResultParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var knowledgeQueryResult = await knowledgeService.QueryKnowledgeAsync(knowledgeQueryParameter.ValueAs(Values[typeof(KnowledgeQueryForCoderParameter)]), cancellationToken);
            return new EWStepExecutionResult
            {
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(KnowledgeQueryForCoderResultParameter), knowledgeQueryResult }
                }
            };
        }
    }
}
