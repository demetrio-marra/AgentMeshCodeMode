using AgentMesh.Application.Models.RequestAnalysis;
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
        LanguageOfTheUserParameter languageOfTheUserParameter) : IEWStep
    {
        public string Name => "Request Analyzer";

        public bool IsAgentic => true;

        public string? AgentName => "RequestAnalyzer";

        public bool IsPipelineFirst => true;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentInput = new RequestAnalyzerAgentInput
            {
                UserLastRequest = userLastRequestParameter.ParameterValue ?? string.Empty,
                ContextMessages = [.. (initialContextMessagesParameter.ParameterValue ?? [])]
            };

            var agentOutput = await requestAnalyzerAgent.ExecuteAsync(agentInput, cancellationToken);

            userIntentParameter.ParameterValue = agentOutput.Intent;
            intentCategoryParameter.ParameterValue = agentOutput.IntentCategory;
            conversationTopicParameter.ParameterValue = agentOutput.ConversationTopic;
            userRequestedActionsParameter.ParameterValue = agentOutput.UserRequestedActions;
            userProvidedDataParameter.ParameterValue = agentOutput.UserProvidedData;
            userPreferencesParameter.ParameterValue = agentOutput.UserPreferences;
            missingValuesParameter.ParameterValue = agentOutput.MissingValues;
            languageOfTheUserParameter.ParameterValue = agentOutput.LanguageOfTheUser;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
