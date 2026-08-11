using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RerankerEWAgenticStep(
        RerankerAgent rerankerAgent,
        RequestDateTimeParameter requestDateTimeParameter,
        UserIntentParameter userIntentParameter,
        ConversationTopicParameter conversationTopicParameter,
        UserRequestedActionsParameter userRequestedActionsParameter,
        UserProvidedDataParameter userProvidedDataParameter,
        UserPreferencesParameter userPreferencesParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
        KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter) : IEWAgenticStep
    {
        public string Name => "Reranker";

        public string? AgentName => "Reranker";

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => false;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await rerankerAgent.ExecuteAsync([
                requestDateTimeParameter,
                knowledgeBaseQueryResultsParameter,
                userIntentParameter,
                conversationTopicParameter,
                userRequestedActionsParameter,
                userProvidedDataParameter,
                userPreferencesParameter,
                pastMemoriesQueryResultsParameter,
                ], cancellationToken);

            // TODO: filter knowledgebase depending on agentOutput file names
            var result = knowledgeBaseQueryResultsParameter.ParameterValue!.Where(p => agentOutput.Result.Contains(p.File, StringComparer.OrdinalIgnoreCase))
                .ToList();

            knowledgeBaseQueryResultsParameter.ParameterValue = result;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
