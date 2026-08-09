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

        private readonly TechnicalAnalystAgent technicalAnalystAgent = technicalAnalystAgent;
        private readonly UserIntentParameter userIntentParameter = userIntentParameter;
        private readonly ConversationTopicParameter conversationTopicParameter = conversationTopicParameter;
        private readonly BusinessRequirementsParameter businessRequirementsParameter = businessRequirementsParameter;
        private readonly UserRequestedActionsParameter userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly UserProvidedDataParameter userProvidedDataParameter = userProvidedDataParameter;
        private readonly UserPreferencesParameter userPreferencesParameter = userPreferencesParameter;
        private readonly PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
        private readonly KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter = knowledgeBaseAPIDocumentsContentParameter;
        private readonly TechnicalSpecificationParameter technicalSpecificationParameter = technicalSpecificationParameter;
        private readonly TechnicalAnalystRejectedParameter technicalAnalystRejectedParameter = technicalAnalystRejectedParameter;
        private readonly TechnicalAnalystRejectReasonsParameter technicalAnalystRejectReasonsParameter = technicalAnalystRejectReasonsParameter;
        private readonly SelectedAPIsFileLocationsParameter selectedAPIsFileLocationsParameter = selectedAPIsFileLocationsParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var kbApiContent = JsonSerializer.Serialize(this.knowledgeBaseAPIDocumentsContentParameter.ParameterValue ?? [], SerializationUtils.DefaultSerializeOptions);

            var agentInput = new TechnicalAnalystAgentInput
            {
                Intent = this.userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = this.conversationTopicParameter.ParameterValue ?? string.Empty,
                BusinessRequirements = this.businessRequirementsParameter.ParameterValue ?? string.Empty,
                UserRequestedActions = this.userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = this.userProvidedDataParameter.ParameterValue ?? [],
                UserPreferences = this.userPreferencesParameter.ParameterValue ?? [],
                AgentMemories = (this.pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbApiContent
            };

            var agentOutput = await this.technicalAnalystAgent.ExecuteAsync(agentInput, cancellationToken);

            this.technicalSpecificationParameter.ParameterValue = agentOutput.TechnicalSpecification;
            this.technicalAnalystRejectedParameter.ParameterValue = agentOutput.RequestRejected;
            this.technicalAnalystRejectReasonsParameter.ParameterValue = agentOutput.ReasonOfRejection;
            this.selectedAPIsFileLocationsParameter.ParameterValue = agentOutput.SelectedAPIsFileLocations;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
