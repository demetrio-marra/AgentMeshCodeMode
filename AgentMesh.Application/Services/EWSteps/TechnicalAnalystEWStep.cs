using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.TechnicalAnalyst;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using AgentMesh.Utils;
using System.Text.Json;

namespace AgentMesh.Application.Services.EWSteps
{
    public class TechnicalAnalystEWStep(
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
        TechnicalAnalystRejectedParameter technicalAnalystRejectedParameter,
        TechnicalAnalystRejectReasonsParameter technicalAnalystRejectReasonsParameter,
        SelectedAPIsFileLocationsParameter selectedAPIsFileLocationsParameter) : IEWStep
    {
        public string Name => "Technical Analyst";

        public bool IsAgentic => true;

        public string? AgentName => TechnicalAnalystAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
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

            technicalSpecificationParameter.ParameterValue = agentOutput.TechnicalSpecification;
            technicalAnalystRejectedParameter.ParameterValue = agentOutput.RequestRejected;
            technicalAnalystRejectReasonsParameter.ParameterValue = agentOutput.ReasonOfRejection;
            selectedAPIsFileLocationsParameter.ParameterValue = agentOutput.SelectedAPIsFileLocations;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
