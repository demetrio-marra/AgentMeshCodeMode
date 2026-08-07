using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.DomainExpert;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Workflows.Steps;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainExpertEWStep(
        DomainExpertAgent domainExpertAgent,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Domain Expert";

        public bool IsAgentic => true;

        public string? AgentName => DomainExpertAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.UserIntent,
            EWParameterNames.ConversationTopic,
            EWParameterNames.UserRequestedActions,
            EWParameterNames.UserProvidedData,
            EWParameterNames.UserPreferences,
            EWParameterNames.PastMemoriesQueryResults,
            EWParameterNames.DomainsKnowledgeBaseDocumentsContent,
            EWParameterNames.SandboxResult,
            EWParameterNames.LanguageOfTheUser
        ];

        private readonly DomainExpertAgent _domainExpertAgent = domainExpertAgent;
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

            var sandboxResultParameter = inputParameters.Single(p => p.Name == EWParameterNames.SandboxResult);
            if (sandboxResultParameter is not SandboxResultParameter typedSandboxResult)
                throw new InvalidOperationException($"Parameter {EWParameterNames.SandboxResult} is not of type SandboxResultParameter");

            var languageParameter = inputParameters.Single(p => p.Name == EWParameterNames.LanguageOfTheUser);
            if (languageParameter is not LanguageOfTheUserParameter typedLanguage)
                throw new InvalidOperationException($"Parameter {EWParameterNames.LanguageOfTheUser} is not of type LanguageOfTheUserParameter");

            var kbContent = WorkflowExecutorFormatting.SerializeDocumentation(typedDocs.ParameterValue ?? []);

            var agentInput = new DomainExpertAgentInput
            {
                Intent = typedIntent.ParameterValue ?? string.Empty,
                ConversationTopic = typedTopic.ParameterValue ?? string.Empty,
                UserRequestedActions = typedActions.ParameterValue ?? [],
                UserProvidedData = typedData.ParameterValue ?? [],
                UserPreferences = typedPreferences.ParameterValue ?? [],
                AgentMemories = (typedMemories.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent,
                DataToComment = typedSandboxResult.ParameterValue ?? string.Empty,
                LanguageOfTheUser = typedLanguage.ParameterValue ?? string.Empty
            };

            var agentOutput = await _domainExpertAgent.ExecuteAsync(agentInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.DomainExpertOutput, agentOutput.DomainExpertComment);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
