using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class FunctionalAnalystEWAgenticStep(
        RequestDateTimeParameter requestDateTimeParameter,
        FunctionalAnalystAgent functionalAnalystAgent,
        UserIntentParameter userIntentParameter,
        ConversationTopicParameter conversationTopicParameter,
        UserRequestedActionsParameter userRequestedActionsParameter,
        UserProvidedDataParameter userProvidedDataParameter,
        UserPreferencesParameter userPreferencesParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
        DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter,
        BusinessRequirementsParameter businessRequirementsParameter,
        RequestRejectedFlagParameter requestRejectedFlagParameter,
        RequestRejectedReasonParameter requestRejectedReasonParameter) : IEWAgenticStep
    {
        public string Name => "Functional Analyst";

        public string? AgentName => "FunctionalAnalyst";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await functionalAnalystAgent.ExecuteAsync([
                requestDateTimeParameter,
                userIntentParameter,
                conversationTopicParameter,
                userRequestedActionsParameter,
                userProvidedDataParameter,
                userPreferencesParameter,
                pastMemoriesQueryResultsParameter,
                domainsKnowledgeBaseDocumentsContentParameter
                ], cancellationToken);

            businessRequirementsParameter.ParameterValue = agentOutput.Result.BusinessRequirements;
            requestRejectedFlagParameter.ParameterValue = agentOutput.Result.RequestRejected;
            if (agentOutput.Result.RequestRejected
                && !string.IsNullOrWhiteSpace(agentOutput.Result.ReasonOfRejection))
            {
                requestRejectedReasonParameter.ParameterValue = agentOutput.Result.ReasonOfRejection;
            }

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
