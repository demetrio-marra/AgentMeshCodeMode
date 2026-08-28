using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class KnowledgeRerankerEWAgenticStep(
        KnowledgeRerankerAgent knowledgeRerankerAgent,
        KnowledgeQueryResultParameter knowledgeQueryResultParameter) : IEWAgenticStep
    {
        public string Name => "Knowledge Reranker";

        public string? AgentName => "KnowledgeReranker";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(UserIntentParameter),
            typeof(ConversationTopicParameter),
            typeof(UserMentionedEntitiesParameter),
            typeof(UserProvidedDataParameter),
            typeof(UserPreferencesParameter),
            typeof(PastMemoriesQueryResultsParameter),
            typeof(KnowledgeQueryResultParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(KnowledgeQueryResultParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await knowledgeRerankerAgent.ExecuteAsync(Values, cancellationToken);

            var initialKnowledgeQueryResult = knowledgeQueryResultParameter.ValueAs(Values[typeof(KnowledgeQueryResultParameter)]) ?? new KnowledgeQueryResult();

            var selectedContentIds = new HashSet<string>(agentOutput.Result.ContentIds, StringComparer.OrdinalIgnoreCase);
            var selectedEntityIds = new HashSet<string>(agentOutput.Result.EntityIds, StringComparer.OrdinalIgnoreCase);
            var selectedRelationIds = new HashSet<string>(agentOutput.Result.RelationIds, StringComparer.OrdinalIgnoreCase);

            var rerankedContents = initialKnowledgeQueryResult.Contents
                .Where(content => selectedContentIds.Contains(content.Id))
                .ToArray();

            var rerankedEntities = initialKnowledgeQueryResult.Entities
                .Where(entity => selectedEntityIds.Contains(entity.Id))
                .ToArray();

            var rerankedRelations = initialKnowledgeQueryResult.Relations
                .Where(relation => selectedRelationIds.Contains(relation.Id))
                .ToArray();

            var result = new KnowledgeQueryResult
            {
                Contents = rerankedContents,
                Entities = rerankedEntities,
                Relations = rerankedRelations
            };

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(KnowledgeQueryResultParameter), result }
                }
            };
        }

            }
        }
