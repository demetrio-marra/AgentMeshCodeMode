using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class CanonicalizerEWAgenticStep(
        CanonicalizerAgent canonicalizerAgent) : IEWAgenticStep
    {
        public string Name => "Request Canonicalization";

        public string? AgentName => "RequestCanonicalization";

        public bool CountInputTokensAsContextTokens => true;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes =>
        [
            typeof(UserIntentParameter),
            typeof(ConversationTopicParameter),
            typeof(UserMentionedEntitiesParameter),
            typeof(UserProvidedDataParameter),
            typeof(UserPreferencesParameter),
            typeof(KnowledgeQueryResultParameter)
        ];

        public IEnumerable<Type> OutputParameterTypes =>
        [
            typeof(UserIntentParameter),
            typeof(IntentCategoryParameter),
            typeof(ConversationTopicParameter),
            typeof(UserMentionedEntitiesParameter),
            typeof(UserProvidedDataParameter),
            typeof(UserPreferencesParameter)
        ];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await canonicalizerAgent.ExecuteAsync(Values, cancellationToken);

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(UserIntentParameter), agentOutput.Result.Intent },
                    { typeof(IntentCategoryParameter), agentOutput.Result.IntentCategory },
                    { typeof(ConversationTopicParameter), agentOutput.Result.ConversationTopic },
                    { typeof(UserMentionedEntitiesParameter), agentOutput.Result.UserMentionedEntities },
                    { typeof(UserProvidedDataParameter), agentOutput.Result.UserProvidedData },
                    { typeof(UserPreferencesParameter), agentOutput.Result.UserPreferences }
                }
            };
        }
    }
}
