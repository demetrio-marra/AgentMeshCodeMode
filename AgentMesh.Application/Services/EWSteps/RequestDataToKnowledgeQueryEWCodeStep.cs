using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RequestDataToKnowledgeQueryEWCodeStep(
        UserIntentParameter userIntentParameter,
        UserMentionedEntitiesParameter userMentionedEntitiesParameter,
        UserProvidedDataParameter userProvidedDataParameter,
        ConversationTopicParameter conversationTopicParameter) : IEWStep
    {
        public string Name => "Request Data To Knowledge Query";

        public IEnumerable<Type> InputParameterTypes =>
        [
            typeof(UserIntentParameter),
            typeof(UserMentionedEntitiesParameter),
            typeof(UserProvidedDataParameter),
            typeof(ConversationTopicParameter)
        ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(KnowledgeQueryParameter)];

        public Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var userIntent = userIntentParameter.ValueAs(Values[typeof(UserIntentParameter)]);
            var userMentionedEntities = userMentionedEntitiesParameter.ValueAs(Values[typeof(UserMentionedEntitiesParameter)]);
            var userProvidedData = userProvidedDataParameter.ValueAs(Values[typeof(UserProvidedDataParameter)]);
            _ = conversationTopicParameter.ValueAs(Values[typeof(ConversationTopicParameter)]);

            var knowledgeQuery = new KnowledgeQuery
            {
                QueryText = userIntent!,
                PrimaryRelevanceKeywords = userMentionedEntities ?? [],
                SecondaryRelevanceKeywords = userProvidedData ?? []
            };

            return Task.FromResult(new EWStepExecutionResult
            {
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(KnowledgeQueryParameter), knowledgeQuery }
                }
            });
        }
    }
}
