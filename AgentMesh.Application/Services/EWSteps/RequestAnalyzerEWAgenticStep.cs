using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RequestAnalyzerEWAgenticStep(
        RequestAnalyzerAgent requestAnalyzerAgent,
        UserLastRequestParameter userLastRequestParameter,
        InitialContextMessagesParameter initialContextMessagesParameter,
        UserIntentParameter userIntentParameter,
        IntentCategoryParameter intentCategoryParameter,
        ConversationTopicParameter conversationTopicParameter,
        UserRequestedActionsParameter userRequestedActionsParameter,
        UserProvidedDataParameter userProvidedDataParameter,
        UserPreferencesParameter userPreferencesParameter,
        MissingValuesParameter missingValuesParameter,
        RequestDateTimeParameter requestDateTimeParameter,
        LanguageOfTheUserParameter languageOfTheUserParameter) : IEWAgenticStep
    {
        public string Name => "Request Analyzer";
        
        public string? AgentName => "RequestAnalyzer";

        public bool CountInputTokensAsContextTokens => true;

        public bool CountOutputTokensAsContextTokens => false;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await requestAnalyzerAgent.ExecuteAsync([userLastRequestParameter, 
                initialContextMessagesParameter, 
                requestDateTimeParameter], cancellationToken);

            userIntentParameter.ParameterValue = agentOutput.Result.Intent;
            intentCategoryParameter.ParameterValue = agentOutput.Result.IntentCategory;
            conversationTopicParameter.ParameterValue = agentOutput.Result.ConversationTopic;
            userRequestedActionsParameter.ParameterValue = agentOutput.Result.UserRequestedActions;
            userProvidedDataParameter.ParameterValue = agentOutput.Result.UserProvidedData;
            userPreferencesParameter.ParameterValue = agentOutput.Result.UserPreferences;
            missingValuesParameter.ParameterValue = agentOutput.Result.MissingValues;
            languageOfTheUserParameter.ParameterValue = agentOutput.Result.LanguageOfTheUser;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
