using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class TechnicalAnalystEWAgenticStep(
        TechnicalAnalystAgent technicalAnalystAgent,
        RequestDateTimeParameter requestDateTimeParameter,
        UserIntentParameter userIntentParameter,
        ConversationTopicParameter conversationTopicParameter,
        BusinessRequirementsParameter businessRequirementsParameter,
        UserRequestedActionsParameter userRequestedActionsParameter,
        UserProvidedDataParameter userProvidedDataParameter,
        UserPreferencesParameter userPreferencesParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
        KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter,
        TechnicalSpecificationParameter technicalSpecificationParameter,
        RequestRejectedFlagParameter requestRejectedFlagParameter,
        RequestRejectedReasonParameter requestRejectedReasonParameter) : IEWAgenticStep
    {
        public string Name => "Technical Analyst";

        public string? AgentName => "TechnicalAnalyst";

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => false;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await technicalAnalystAgent.ExecuteAsync([
                requestDateTimeParameter,
                knowledgeBaseAPIDocumentsContentParameter,
                userIntentParameter,
                conversationTopicParameter,
                businessRequirementsParameter,
                userRequestedActionsParameter,
                userPreferencesParameter,
                userProvidedDataParameter,
                pastMemoriesQueryResultsParameter
                ], cancellationToken);

            if (agentOutput.Result.FilteredApisDocumentationFiles != null
                && agentOutput.Result.FilteredApisDocumentationFiles.Any())
            {
                var selectedDocuments = agentOutput.Result.FilteredApisDocumentationFiles.ToList();
                var filteredKbDocuments = (knowledgeBaseAPIDocumentsContentParameter.ParameterValue ?? [])
                    .Where(doc => selectedDocuments.Contains(doc.File, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                knowledgeBaseAPIDocumentsContentParameter.ParameterValue = filteredKbDocuments;
            }

            technicalSpecificationParameter.ParameterValue = agentOutput.Result.TechnicalSpecification;
            requestRejectedFlagParameter.ParameterValue = agentOutput.Result.RequestRejected;
            requestRejectedReasonParameter.ParameterValue = agentOutput.Result.RequestRejectionReason;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
