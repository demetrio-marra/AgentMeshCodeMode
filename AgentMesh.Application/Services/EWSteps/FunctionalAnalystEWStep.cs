using AgentMesh.Application.Models.FunctionalAnalyst;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
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

        public string? AgentName => "FunctionalAnalyst";

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var docsContent = domainsKnowledgeBaseDocumentsContentParameter.ParameterValue ?? [];
            var kbContent = JsonSerializer.Serialize(docsContent, SerializationUtils.DefaultSerializeOptions);

            var agentInput = new FunctionalAnalystAgentInput
            {
                Intent = userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = conversationTopicParameter.ParameterValue ?? string.Empty,
                UserRequestedActions = userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = userProvidedDataParameter.ParameterValue ?? [],
                UserPreferences = userPreferencesParameter.ParameterValue ?? [],
                AgentMemories = (pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent
            };

            var agentOutput = await functionalAnalystAgent.ExecuteAsync(agentInput, cancellationToken);

            businessRequirementsParameter.ParameterValue = agentOutput.BusinessRequirements;
            functionalAnalystRejectedParameter.ParameterValue = agentOutput.RequestRejected;
            functionalAnalystRejectReasonsParameter.ParameterValue = agentOutput.ReasonOfRejection;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
