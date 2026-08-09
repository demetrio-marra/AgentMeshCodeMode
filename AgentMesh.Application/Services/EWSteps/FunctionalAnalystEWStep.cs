using AgentMesh.Application.Models.FunctionalAnalyst;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using AgentMesh.Application.Configuration;
using AgentMesh.Utils;
using System.Text.Json;

namespace AgentMesh.Application.Services.EWSteps
{
    public class FunctionalAnalystEWStep(
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
        FunctionalAnalystRejectReasonsParameter functionalAnalystRejectReasonsParameter) : IEWStep
    {
        public string Name => "Functional Analyst";

        public bool IsAgentic => true;

        public string? AgentName => FunctionalAnalystAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly FunctionalAnalystAgent functionalAnalystAgent = functionalAnalystAgent;
        private readonly UserIntentParameter userIntentParameter = userIntentParameter;
        private readonly ConversationTopicParameter conversationTopicParameter = conversationTopicParameter;
        private readonly UserRequestedActionsParameter userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly UserProvidedDataParameter userProvidedDataParameter = userProvidedDataParameter;
        private readonly UserPreferencesParameter userPreferencesParameter = userPreferencesParameter;
        private readonly PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
        private readonly DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter = domainsKnowledgeBaseDocumentsContentParameter;
        private readonly BusinessRequirementsParameter businessRequirementsParameter = businessRequirementsParameter;
        private readonly FunctionalAnalystRejectedParameter functionalAnalystRejectedParameter = functionalAnalystRejectedParameter;
        private readonly FunctionalAnalystRejectReasonsParameter functionalAnalystRejectReasonsParameter = functionalAnalystRejectReasonsParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var docsContent = this.domainsKnowledgeBaseDocumentsContentParameter.ParameterValue ?? [];
            var kbContent = JsonSerializer.Serialize(docsContent, SerializationUtils.DefaultSerializeOptions);

            var agentInput = new FunctionalAnalystAgentInput
            {
                Intent = this.userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = this.conversationTopicParameter.ParameterValue ?? string.Empty,
                UserRequestedActions = this.userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = this.userProvidedDataParameter.ParameterValue ?? [],
                UserPreferences = this.userPreferencesParameter.ParameterValue ?? [],
                AgentMemories = (this.pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent
            };

            var agentOutput = await this.functionalAnalystAgent.ExecuteAsync(agentInput, cancellationToken);

            this.businessRequirementsParameter.ParameterValue = agentOutput.BusinessRequirements;
            this.functionalAnalystRejectedParameter.ParameterValue = agentOutput.RequestRejected;
            this.functionalAnalystRejectReasonsParameter.ParameterValue = agentOutput.ReasonOfRejection;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
