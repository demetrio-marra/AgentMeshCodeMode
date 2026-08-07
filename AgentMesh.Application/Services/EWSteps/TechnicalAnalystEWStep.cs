using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.TechnicalAnalyst;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Workflows.Steps;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class TechnicalAnalystEWStep(
        TechnicalAnalystAgent technicalAnalystAgent,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Technical Analyst";

        public bool IsAgentic => true;

        public string? AgentName => TechnicalAnalystAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.UserIntent,
            EWParameterNames.ConversationTopic,
            EWParameterNames.BusinessRequirements,
            EWParameterNames.UserRequestedActions,
            EWParameterNames.UserProvidedData,
            EWParameterNames.UserPreferences,
            EWParameterNames.PastMemoriesQueryResults,
            EWParameterNames.KnowledgeBaseAPIDocumentsContent
        ];

        private readonly TechnicalAnalystAgent _technicalAnalystAgent = technicalAnalystAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var intentParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserIntent);
            if (intentParameter is not UserIntentParameter typedIntent)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserIntent} is not of type UserIntentParameter");

            var topicParameter = inputParameters.Single(p => p.Name == EWParameterNames.ConversationTopic);
            if (topicParameter is not ConversationTopicParameter typedTopic)
                throw new InvalidOperationException($"Parameter {EWParameterNames.ConversationTopic} is not of type ConversationTopicParameter");

            var requirementsParameter = inputParameters.Single(p => p.Name == EWParameterNames.BusinessRequirements);
            if (requirementsParameter is not BusinessRequirementsParameter typedRequirements)
                throw new InvalidOperationException($"Parameter {EWParameterNames.BusinessRequirements} is not of type BusinessRequirementsParameter");

            var actionsParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserRequestedActions);
            if (actionsParameter is not UserRequestedActionsParameter typedActions)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserRequestedActions} is not of type UserRequestedActionsParameter");

            var dataParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserProvidedData);
            if (dataParameter is not UserProvidedDataParameter typedData)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserProvidedData} is not of type UserProvidedDataParameter");

            var preferencesParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserPreferences);
            if (preferencesParameter is not UserPreferencesParameter typedPreferences)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserPreferences} is not of type UserPreferencesParameter");

            var memoriesParameter = inputParameters.Single(p => p.Name == EWParameterNames.PastMemoriesQueryResults);
            if (memoriesParameter is not PastMemoriesQueryResultsParameter typedMemories)
                throw new InvalidOperationException($"Parameter {EWParameterNames.PastMemoriesQueryResults} is not of type PastMemoriesQueryResultsParameter");

            var apiDocsParameter = inputParameters.Single(p => p.Name == EWParameterNames.KnowledgeBaseAPIDocumentsContent);
            if (apiDocsParameter is not KnowledgeBaseAPIDocumentsContentParameter typedApiDocs)
                throw new InvalidOperationException($"Parameter {EWParameterNames.KnowledgeBaseAPIDocumentsContent} is not of type KnowledgeBaseAPIDocumentsContentParameter");

            var kbApiContent = WorkflowExecutorFormatting.SerializeDocumentation(typedApiDocs.ParameterValue ?? []);

            var agentInput = new TechnicalAnalystAgentInput
            {
                Intent = typedIntent.ParameterValue ?? string.Empty,
                ConversationTopic = typedTopic.ParameterValue ?? string.Empty,
                BusinessRequirements = typedRequirements.ParameterValue ?? string.Empty,
                UserRequestedActions = typedActions.ParameterValue ?? [],
                UserProvidedData = typedData.ParameterValue ?? [],
                UserPreferences = typedPreferences.ParameterValue ?? [],
                AgentMemories = (typedMemories.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbApiContent
            };

            var agentOutput = await _technicalAnalystAgent.ExecuteAsync(agentInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.TechnicalSpecification, agentOutput.TechnicalSpecification);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.TechnicalAnalystRejected, agentOutput.RequestRejected);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.TechnicalAnalystRejectReasons, agentOutput.ReasonOfRejection);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.SelectedAPIsFileLocations, agentOutput.SelectedAPIsFileLocations);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
