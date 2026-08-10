using AgentMesh.Application.Models.DomainExpert;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using AgentMesh.Utils;
using System.Text.Json;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainExpertEWAgenticStep(
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
        DomainExpertOutputParameter domainExpertOutputParameter) : IEWAgenticStep
    {
        public string Name => "Domain Expert";

        public string? AgentName => "DomainExpert";

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var kbContent = JsonSerializer.Serialize(domainsKnowledgeBaseDocumentsContentParameter.ParameterValue ?? [], SerializationUtils.DefaultSerializeOptions);

            var agentInput = new DomainExpertAgentInput
            {
                Intent = userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = conversationTopicParameter.ParameterValue ?? string.Empty,
                UserRequestedActions = userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = userProvidedDataParameter.ParameterValue ?? [],
                UserPreferences = userPreferencesParameter.ParameterValue ?? [],
                AgentMemories = (pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent,
                DataToComment = sandboxResultParameter.ParameterValue ?? string.Empty,
                LanguageOfTheUser = languageOfTheUserParameter.ParameterValue ?? string.Empty
            };

            var agentOutput = await domainExpertAgent.ExecuteAsync(agentInput, cancellationToken);

            domainExpertOutputParameter.ParameterValue = agentOutput.DomainExpertComment;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
