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

        private readonly TechnicalAnalystAgent _technicalAnalystAgent = technicalAnalystAgent;
        private readonly UserIntentParameter _userIntentParameter = userIntentParameter;
        private readonly ConversationTopicParameter _conversationTopicParameter = conversationTopicParameter;
        private readonly BusinessRequirementsParameter _businessRequirementsParameter = businessRequirementsParameter;
        private readonly UserRequestedActionsParameter _userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly UserProvidedDataParameter _userProvidedDataParameter = userProvidedDataParameter;
        private readonly UserPreferencesParameter _userPreferencesParameter = userPreferencesParameter;
        private readonly PastMemoriesQueryResultsParameter _pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
        private readonly KnowledgeBaseAPIDocumentsContentParameter _knowledgeBaseAPIDocumentsContentParameter = knowledgeBaseAPIDocumentsContentParameter;
        private readonly TechnicalSpecificationParameter _technicalSpecificationParameter = technicalSpecificationParameter;
        private readonly TechnicalAnalystRejectedParameter _technicalAnalystRejectedParameter = technicalAnalystRejectedParameter;
        private readonly TechnicalAnalystRejectReasonsParameter _technicalAnalystRejectReasonsParameter = technicalAnalystRejectReasonsParameter;
        private readonly SelectedAPIsFileLocationsParameter _selectedAPIsFileLocationsParameter = selectedAPIsFileLocationsParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var kbApiContent = JsonSerializer.Serialize(_knowledgeBaseAPIDocumentsContentParameter.ParameterValue ?? [], SerializationUtils.DefaultSerializeOptions);

            var agentInput = new TechnicalAnalystAgentInput
            {
                Intent = _userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = _conversationTopicParameter.ParameterValue ?? string.Empty,
                BusinessRequirements = _businessRequirementsParameter.ParameterValue ?? string.Empty,
                UserRequestedActions = _userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = _userProvidedDataParameter.ParameterValue ?? [],
                UserPreferences = _userPreferencesParameter.ParameterValue ?? [],
                AgentMemories = (_pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbApiContent
            };

            var agentOutput = await _technicalAnalystAgent.ExecuteAsync(agentInput, cancellationToken);

            _technicalSpecificationParameter.ParameterValue = agentOutput.TechnicalSpecification;
            _technicalAnalystRejectedParameter.ParameterValue = agentOutput.RequestRejected;
            _technicalAnalystRejectReasonsParameter.ParameterValue = agentOutput.ReasonOfRejection;
            _selectedAPIsFileLocationsParameter.ParameterValue = agentOutput.SelectedAPIsFileLocations;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
