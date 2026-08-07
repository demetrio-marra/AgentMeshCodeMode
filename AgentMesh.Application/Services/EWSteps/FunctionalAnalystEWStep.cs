using AgentMesh.Application.Models.FunctionalAnalyst;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Services.Workflows.Steps;

namespace AgentMesh.Application.Services.EWSteps
{
    public class FunctionalAnalystEWStep(
        FunctionalAnalystAgent functionalAnalystAgent,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Functional Analyst";

        public bool IsAgentic => true;

        public string? AgentName => FunctionalAnalystAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.UserIntent,
            EWParameterNames.ConversationTopic,
            EWParameterNames.UserRequestedActions,
            EWParameterNames.UserProvidedData,
            EWParameterNames.UserPreferences,
            EWParameterNames.PastMemoriesQueryResults,
            EWParameterNames.DomainsKnowledgeBaseDocumentsContent
        ];

        private readonly FunctionalAnalystAgent _functionalAnalystAgent = functionalAnalystAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var intentParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserIntent);
            if (intentParameter is not UserIntentParameter typedIntent)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserIntent} is not of type UserIntentParameter");

            var topicParameter = inputParameters.Single(p => p.Name == EWParameterNames.ConversationTopic);
            if (topicParameter is not ConversationTopicParameter typedTopic)
                throw new InvalidOperationException($"Parameter {EWParameterNames.ConversationTopic} is not of type ConversationTopicParameter");

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

            var docsParameter = inputParameters.Single(p => p.Name == EWParameterNames.DomainsKnowledgeBaseDocumentsContent);
            if (docsParameter is not DomainsKnowledgeBaseDocumentsContentParameter typedDocs)
                throw new InvalidOperationException($"Parameter {EWParameterNames.DomainsKnowledgeBaseDocumentsContent} is not of type DomainsKnowledgeBaseDocumentsContentParameter");

            var docsContent = typedDocs.ParameterValue ?? [];
            var kbContent = WorkflowExecutorFormatting.SerializeDocumentation(docsContent);

            var agentInput = new FunctionalAnalystAgentInput
            {
                Intent = typedIntent.ParameterValue ?? string.Empty,
                ConversationTopic = typedTopic.ParameterValue ?? string.Empty,
                UserRequestedActions = typedActions.ParameterValue ?? [],
                UserProvidedData = typedData.ParameterValue ?? [],
                UserPreferences = typedPreferences.ParameterValue ?? [],
                AgentMemories = (typedMemories.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent
            };

            var agentOutput = await _functionalAnalystAgent.ExecuteAsync(agentInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.BusinessRequirements, agentOutput.BusinessRequirements);
            _ewParametersProvider.UpdateParameterValue<bool?>(EWParameterNames.FunctionalAnalystRejected, agentOutput.RequestRejected);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.FunctionalAnalystRejectReasons, agentOutput.ReasonOfRejection);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.ShouldEngageCoder, !agentOutput.RequestRejected);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
