using AgentMesh.Application.Models.RequestAnalysis;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RequestAnalyzerEWStep(
        RequestAnalyzerAgent requestAnalyzerAgent,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Request Analyzer";

        public bool IsAgentic => true;

        public string? AgentName => RequestAnalyzerAgentConfiguration.AgentName;

        public bool IsPipelineFirst => true;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.UserLastRequest,
            EWParameterNames.InitialContextMessages
        ];

        private readonly RequestAnalyzerAgent _requestAnalyzerAgent = requestAnalyzerAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var userLastRequestParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserLastRequest);
            if (userLastRequestParameter is not UserLastRequestParameter typedUserLastRequest)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserLastRequest} is not of type UserLastRequestParameter");

            var contextMessagesParameter = inputParameters.Single(p => p.Name == EWParameterNames.InitialContextMessages);
            if (contextMessagesParameter is not InitialContextMessagesParameter typedContextMessages)
                throw new InvalidOperationException($"Parameter {EWParameterNames.InitialContextMessages} is not of type InitialContextMessagesParameter");

            var agentInput = new RequestAnalyzerAgentInput
            {
                UserLastRequest = typedUserLastRequest.ParameterValue ?? string.Empty,
                ContextMessages = [.. (typedContextMessages.ParameterValue ?? [])]
            };

            var agentOutput = await _requestAnalyzerAgent.ExecuteAsync(agentInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.UserIntent, agentOutput.Intent);
            _ewParametersProvider.UpdateParameterValue<AgentMesh.Models.RequestAnalysis.UserIntentCategory?>(EWParameterNames.IntentCategory, agentOutput.IntentCategory);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.ConversationTopic, agentOutput.ConversationTopic);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.UserRequestedActions, agentOutput.UserRequestedActions);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.UserProvidedData, agentOutput.UserProvidedData);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.UserPreferences, agentOutput.UserPreferences);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.MissingValues, agentOutput.MissingValues);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.LanguageOfTheUser, agentOutput.LanguageOfTheUser);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
