using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RequestAnalyzerEWAgenticStep(
        RequestAnalyzerAgent requestAnalyzerAgent) : IEWAgenticStep
    {
        public string Name => "Request Analyzer";

        public string? AgentName => "RequestAnalyzer";

        public bool CountInputTokensAsContextTokens => true;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(UserLastRequestParameter),
            typeof(InitialContextMessagesParameter),
            typeof(RequestDateTimeParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [
            typeof(UserIntentParameter),
            typeof(IntentCategoryParameter),
            typeof(IsSmallTalkParameter),
            typeof(ConversationTopicParameter),
            typeof(UserMentionedEntitiesParameter),
            typeof(UserProvidedDataParameter),
            typeof(UserPreferencesParameter),
            typeof(MissingValuesParameter),
            typeof(LanguageOfTheUserParameter)
            ];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await requestAnalyzerAgent.ExecuteAsync(Values, cancellationToken);

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(UserIntentParameter), agentOutput.Result.Intent },
                    { typeof(IntentCategoryParameter), agentOutput.Result.IntentCategory },
                    { typeof(IsSmallTalkParameter), agentOutput.Result.IsSmallTalk },
                    { typeof(ConversationTopicParameter), agentOutput.Result.ConversationTopic },
                    { typeof(UserMentionedEntitiesParameter), agentOutput.Result.UserMentionedEntities },
                    { typeof(UserProvidedDataParameter), agentOutput.Result.UserProvidedData },
                    { typeof(UserPreferencesParameter), agentOutput.Result.UserPreferences },
                    { typeof(MissingValuesParameter), agentOutput.Result.MissingValues },
                    { typeof(LanguageOfTheUserParameter), agentOutput.Result.LanguageOfTheUser }
                }
            };
        }
    }
}
