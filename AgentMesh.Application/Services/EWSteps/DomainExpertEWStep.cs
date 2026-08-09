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

        private readonly DomainExpertAgent domainExpertAgent = domainExpertAgent;
        private readonly UserIntentParameter userIntentParameter = userIntentParameter;
        private readonly ConversationTopicParameter conversationTopicParameter = conversationTopicParameter;
        private readonly UserRequestedActionsParameter userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly UserProvidedDataParameter userProvidedDataParameter = userProvidedDataParameter;
        private readonly UserPreferencesParameter userPreferencesParameter = userPreferencesParameter;
        private readonly PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
        private readonly DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter = domainsKnowledgeBaseDocumentsContentParameter;
        private readonly SandboxResultParameter sandboxResultParameter = sandboxResultParameter;
        private readonly LanguageOfTheUserParameter languageOfTheUserParameter = languageOfTheUserParameter;
        private readonly DomainExpertOutputParameter domainExpertOutputParameter = domainExpertOutputParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var kbContent = JsonSerializer.Serialize(this.domainsKnowledgeBaseDocumentsContentParameter.ParameterValue ?? [], SerializationUtils.DefaultSerializeOptions);

            var agentInput = new DomainExpertAgentInput
            {
                Intent = this.userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = this.conversationTopicParameter.ParameterValue ?? string.Empty,
                UserRequestedActions = this.userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = this.userProvidedDataParameter.ParameterValue ?? [],
                UserPreferences = this.userPreferencesParameter.ParameterValue ?? [],
                AgentMemories = (this.pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent,
                DataToComment = this.sandboxResultParameter.ParameterValue ?? string.Empty,
                LanguageOfTheUser = this.languageOfTheUserParameter.ParameterValue ?? string.Empty
            };

            var agentOutput = await this.domainExpertAgent.ExecuteAsync(agentInput, cancellationToken);

            this.domainExpertOutputParameter.ParameterValue = agentOutput.DomainExpertComment;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
