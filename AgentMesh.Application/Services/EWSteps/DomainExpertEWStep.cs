using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.DomainExpert;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using AgentMesh.Utils;
using System.Text.Json;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainExpertEWStep(
        DomainExpertAgent domainExpertAgent,
        UserIntentParameter userIntentParameter,
        ConversationTopicParameter conversationTopicParameter,
        UserRequestedActionsParameter userRequestedActionsParameter,
        UserProvidedDataParameter userProvidedDataParameter,
        UserPreferencesParameter userPreferencesParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
        DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter,
        SandboxResultParameter sandboxResultParameter,
        LanguageOfTheUserParameter languageOfTheUserParameter,
        DomainExpertOutputParameter domainExpertOutputParameter) : IEWStep
    {
        public string Name => "Domain Expert";

        public bool IsAgentic => true;

        public string? AgentName => DomainExpertAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly DomainExpertAgent _domainExpertAgent = domainExpertAgent;
        private readonly UserIntentParameter _userIntentParameter = userIntentParameter;
        private readonly ConversationTopicParameter _conversationTopicParameter = conversationTopicParameter;
        private readonly UserRequestedActionsParameter _userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly UserProvidedDataParameter _userProvidedDataParameter = userProvidedDataParameter;
        private readonly UserPreferencesParameter _userPreferencesParameter = userPreferencesParameter;
        private readonly PastMemoriesQueryResultsParameter _pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
        private readonly DomainsKnowledgeBaseDocumentsContentParameter _domainsKnowledgeBaseDocumentsContentParameter = domainsKnowledgeBaseDocumentsContentParameter;
        private readonly SandboxResultParameter _sandboxResultParameter = sandboxResultParameter;
        private readonly LanguageOfTheUserParameter _languageOfTheUserParameter = languageOfTheUserParameter;
        private readonly DomainExpertOutputParameter _domainExpertOutputParameter = domainExpertOutputParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var kbContent = JsonSerializer.Serialize(_domainsKnowledgeBaseDocumentsContentParameter.ParameterValue ?? [], SerializationUtils.DefaultSerializeOptions);

            var agentInput = new DomainExpertAgentInput
            {
                Intent = _userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = _conversationTopicParameter.ParameterValue ?? string.Empty,
                UserRequestedActions = _userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = _userProvidedDataParameter.ParameterValue ?? [],
                UserPreferences = _userPreferencesParameter.ParameterValue ?? [],
                AgentMemories = (_pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent,
                DataToComment = _sandboxResultParameter.ParameterValue ?? string.Empty,
                LanguageOfTheUser = _languageOfTheUserParameter.ParameterValue ?? string.Empty
            };

            var agentOutput = await _domainExpertAgent.ExecuteAsync(agentInput, cancellationToken);

            _domainExpertOutputParameter.ParameterValue = agentOutput.DomainExpertComment;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
