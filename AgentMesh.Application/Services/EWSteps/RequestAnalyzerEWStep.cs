using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RequestAnalyzerEWStep(
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
        LanguageOfTheUserParameter languageOfTheUserParameter) : IEWStep
    {
        public string Name => "Request Analyzer";

        public bool IsAgentic => true;

        public string? AgentName => "RequestAnalyzer";

        public bool IsPipelineFirst => true;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
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

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
