using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class KnowledgeForCoderRerankerEWAgenticStep(
        KnowledgeForCoderRerankerAgent knowledgeForCoderRerankerAgent,
        KnowledgeQueryForCoderResultParameter knowledgeQueryForCoderResultParameter) : IEWAgenticStep
    {
        public string Name => "Knowledge For Coder Reranker";

        public string? AgentName => "KnowledgeForCoderReranker";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(KnowledgeQueryForCoderParameter),
            typeof(KnowledgeQueryForCoderResultParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(KnowledgeContentForCoderParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await knowledgeForCoderRerankerAgent.ExecuteAsync(Values, cancellationToken);

            var selectedContentIds = new HashSet<string>(agentOutput.Result.ContentIds, StringComparer.OrdinalIgnoreCase);
            var selectedEntityIds = new HashSet<string>(agentOutput.Result.EntityIds, StringComparer.OrdinalIgnoreCase);
            var selectedRelationIds = new HashSet<string>(agentOutput.Result.RelationIds, StringComparer.OrdinalIgnoreCase);

            var initialKnowledgeQueryResult = knowledgeQueryForCoderResultParameter.ValueAs(Values[typeof(KnowledgeQueryForCoderResultParameter)]) ?? new KnowledgeQueryResult();

            var rerankedContents = initialKnowledgeQueryResult.Contents
                .Where(content => selectedContentIds.Contains(content.Id))
                .ToArray();

            var rerankedEntities = initialKnowledgeQueryResult.Entities
                .Where(entity => selectedEntityIds.Contains(entity.Id))
                .ToArray();

            var rerankedRelations = initialKnowledgeQueryResult.Relations
                .Where(relation => selectedRelationIds.Contains(relation.Id))
                .ToArray();

            var result = rerankedContents.ToList();

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(KnowledgeContentForCoderParameter), result }
                }
            };
        }
    }
}
