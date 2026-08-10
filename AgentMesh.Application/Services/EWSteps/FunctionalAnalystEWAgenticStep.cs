using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
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
        FunctionalAnalystRejectedParameter functionalAnalystRejectedParameter,
        FunctionalAnalystRejectReasonsParameter functionalAnalystRejectReasonsParameter) : IEWAgenticStep
    {
        public string Name => "Functional Analyst";

        public string? AgentName => "FunctionalAnalyst";

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => false;

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
            functionalAnalystRejectedParameter.ParameterValue = agentOutput.Result.RequestRejected;
            functionalAnalystRejectReasonsParameter.ParameterValue = agentOutput.Result.ReasonOfRejection;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
