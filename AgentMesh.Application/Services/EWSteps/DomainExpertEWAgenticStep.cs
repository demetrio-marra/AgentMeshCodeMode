using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainExpertEWAgenticStep(
        RequestDateTimeParameter requestDateTimeParameter,
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

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await domainExpertAgent.ExecuteAsync([
                requestDateTimeParameter,
                userIntentParameter,
                conversationTopicParameter,
                userRequestedActionsParameter,
                userProvidedDataParameter,
                userPreferencesParameter,
                pastMemoriesQueryResultsParameter,
                domainsKnowledgeBaseDocumentsContentParameter,
                sandboxResultParameter,
                languageOfTheUserParameter
                ], cancellationToken);

            domainExpertOutputParameter.ParameterValue = agentOutput.Result;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
