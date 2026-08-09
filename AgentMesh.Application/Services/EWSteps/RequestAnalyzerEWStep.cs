using AgentMesh.Application.Models.RequestAnalysis;
using AgentMesh.Application.Models.Workflows.Parameters;
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

        public string? AgentName => RequestAnalyzerAgentConfiguration.AgentName;

        public bool IsPipelineFirst => true;

        public bool IsPipelineLast => false;

        private readonly RequestAnalyzerAgent requestAnalyzerAgent = requestAnalyzerAgent;
        private readonly UserLastRequestParameter userLastRequestParameter = userLastRequestParameter;
        private readonly InitialContextMessagesParameter initialContextMessagesParameter = initialContextMessagesParameter;
        private readonly UserIntentParameter userIntentParameter = userIntentParameter;
        private readonly IntentCategoryParameter intentCategoryParameter = intentCategoryParameter;
        private readonly ConversationTopicParameter conversationTopicParameter = conversationTopicParameter;
        private readonly UserRequestedActionsParameter userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly UserProvidedDataParameter userProvidedDataParameter = userProvidedDataParameter;
        private readonly UserPreferencesParameter userPreferencesParameter = userPreferencesParameter;
        private readonly MissingValuesParameter missingValuesParameter = missingValuesParameter;
        private readonly LanguageOfTheUserParameter languageOfTheUserParameter = languageOfTheUserParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentInput = new RequestAnalyzerAgentInput
            {
                UserLastRequest = this.userLastRequestParameter.ParameterValue ?? string.Empty,
                ContextMessages = [.. (this.initialContextMessagesParameter.ParameterValue ?? [])]
            };

            var agentOutput = await this.requestAnalyzerAgent.ExecuteAsync(agentInput, cancellationToken);

            this.userIntentParameter.ParameterValue = agentOutput.Intent;
            this.intentCategoryParameter.ParameterValue = agentOutput.IntentCategory;
            this.conversationTopicParameter.ParameterValue = agentOutput.ConversationTopic;
            this.userRequestedActionsParameter.ParameterValue = agentOutput.UserRequestedActions;
            this.userProvidedDataParameter.ParameterValue = agentOutput.UserProvidedData;
            this.userPreferencesParameter.ParameterValue = agentOutput.UserPreferences;
            this.missingValuesParameter.ParameterValue = agentOutput.MissingValues;
            this.languageOfTheUserParameter.ParameterValue = agentOutput.LanguageOfTheUser;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
