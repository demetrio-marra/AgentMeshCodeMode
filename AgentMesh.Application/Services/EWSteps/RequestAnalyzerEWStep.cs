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

        private readonly RequestAnalyzerAgent _requestAnalyzerAgent = requestAnalyzerAgent;
        private readonly UserLastRequestParameter _userLastRequestParameter = userLastRequestParameter;
        private readonly InitialContextMessagesParameter _initialContextMessagesParameter = initialContextMessagesParameter;
        private readonly UserIntentParameter _userIntentParameter = userIntentParameter;
        private readonly IntentCategoryParameter _intentCategoryParameter = intentCategoryParameter;
        private readonly ConversationTopicParameter _conversationTopicParameter = conversationTopicParameter;
        private readonly UserRequestedActionsParameter _userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly UserProvidedDataParameter _userProvidedDataParameter = userProvidedDataParameter;
        private readonly UserPreferencesParameter _userPreferencesParameter = userPreferencesParameter;
        private readonly MissingValuesParameter _missingValuesParameter = missingValuesParameter;
        private readonly LanguageOfTheUserParameter _languageOfTheUserParameter = languageOfTheUserParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentInput = new RequestAnalyzerAgentInput
            {
                UserLastRequest = _userLastRequestParameter.ParameterValue ?? string.Empty,
                ContextMessages = [.. (_initialContextMessagesParameter.ParameterValue ?? [])]
            };

            var agentOutput = await _requestAnalyzerAgent.ExecuteAsync(agentInput, cancellationToken);

            _userIntentParameter.ParameterValue = agentOutput.Intent;
            _intentCategoryParameter.ParameterValue = agentOutput.IntentCategory;
            _conversationTopicParameter.ParameterValue = agentOutput.ConversationTopic;
            _userRequestedActionsParameter.ParameterValue = agentOutput.UserRequestedActions;
            _userProvidedDataParameter.ParameterValue = agentOutput.UserProvidedData;
            _userPreferencesParameter.ParameterValue = agentOutput.UserPreferences;
            _missingValuesParameter.ParameterValue = agentOutput.MissingValues;
            _languageOfTheUserParameter.ParameterValue = agentOutput.LanguageOfTheUser;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
