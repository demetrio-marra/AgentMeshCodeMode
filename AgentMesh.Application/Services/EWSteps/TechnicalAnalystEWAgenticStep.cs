using AgentMesh.Application.Models.TechnicalAnalyst;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using AgentMesh.Utils;
using System.Text.Json;

namespace AgentMesh.Application.Services.EWSteps
{
    public class TechnicalAnalystEWAgenticStep(
        TechnicalAnalystAgent technicalAnalystAgent,
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
            var kbApiContent = JsonSerializer.Serialize(knowledgeBaseAPIDocumentsContentParameter.ParameterValue ?? [], SerializationUtils.DefaultSerializeOptions);

            var agentInput = new TechnicalAnalystAgentInput
            {
                Intent = userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = conversationTopicParameter.ParameterValue ?? string.Empty,
                BusinessRequirements = businessRequirementsParameter.ParameterValue ?? string.Empty,
                UserRequestedActions = userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = userProvidedDataParameter.ParameterValue ?? [],
                UserPreferences = userPreferencesParameter.ParameterValue ?? [],
                AgentMemories = (pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbApiContent
            };

            var agentOutput = await technicalAnalystAgent.ExecuteAsync(agentInput, cancellationToken);

            var filteredKbDocuments = (knowledgeBaseAPIDocumentsContentParameter.ParameterValue ?? [])
                .Where(doc => agentOutput.SelectedAPIsFileLocations.Contains(doc.File))
                .ToList();

            technicalSpecificationParameter.ParameterValue = agentOutput.TechnicalSpecification;
            requestRejectedFlagParameter.ParameterValue = agentOutput.RequestRejected;

            if (agentOutput.RequestRejected
               && !string.IsNullOrWhiteSpace(agentOutput.ReasonOfRejection))
            {
                requestRejectedReasonParameter.ParameterValue = agentOutput.ReasonOfRejection;
            }

            knowledgeBaseAPIDocumentsContentParameter.ParameterValue = filteredKbDocuments;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
