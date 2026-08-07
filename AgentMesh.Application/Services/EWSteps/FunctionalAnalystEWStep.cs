using AgentMesh.Application.Models.FunctionalAnalyst;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Services.Workflows.Steps;

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
        FunctionalAnalystRejectReasonsParameter functionalAnalystRejectReasonsParameter,
        ShouldEngageCoderParameter shouldEngageCoderParameter) : IEWStep
    {
        public string Name => "Functional Analyst";

        public bool IsAgentic => true;

        public string? AgentName => FunctionalAnalystAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly FunctionalAnalystAgent _functionalAnalystAgent = functionalAnalystAgent;
        private readonly UserIntentParameter _userIntentParameter = userIntentParameter;
        private readonly ConversationTopicParameter _conversationTopicParameter = conversationTopicParameter;
        private readonly UserRequestedActionsParameter _userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly UserProvidedDataParameter _userProvidedDataParameter = userProvidedDataParameter;
        private readonly UserPreferencesParameter _userPreferencesParameter = userPreferencesParameter;
        private readonly PastMemoriesQueryResultsParameter _pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
        private readonly DomainsKnowledgeBaseDocumentsContentParameter _domainsKnowledgeBaseDocumentsContentParameter = domainsKnowledgeBaseDocumentsContentParameter;
        private readonly BusinessRequirementsParameter _businessRequirementsParameter = businessRequirementsParameter;
        private readonly FunctionalAnalystRejectedParameter _functionalAnalystRejectedParameter = functionalAnalystRejectedParameter;
        private readonly FunctionalAnalystRejectReasonsParameter _functionalAnalystRejectReasonsParameter = functionalAnalystRejectReasonsParameter;
        private readonly ShouldEngageCoderParameter _shouldEngageCoderParameter = shouldEngageCoderParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var docsContent = _domainsKnowledgeBaseDocumentsContentParameter.ParameterValue ?? [];
            var kbContent = WorkflowExecutorFormatting.SerializeDocumentation(docsContent);

            var agentInput = new FunctionalAnalystAgentInput
            {
                Intent = _userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = _conversationTopicParameter.ParameterValue ?? string.Empty,
                UserRequestedActions = _userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = _userProvidedDataParameter.ParameterValue ?? [],
                UserPreferences = _userPreferencesParameter.ParameterValue ?? [],
                AgentMemories = (_pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent
            };

            var agentOutput = await _functionalAnalystAgent.ExecuteAsync(agentInput, cancellationToken);

            _businessRequirementsParameter.ParameterValue = agentOutput.BusinessRequirements;
            _functionalAnalystRejectedParameter.ParameterValue = agentOutput.RequestRejected;
            _functionalAnalystRejectReasonsParameter.ParameterValue = agentOutput.ReasonOfRejection;
            _shouldEngageCoderParameter.ParameterValue = !agentOutput.RequestRejected;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
