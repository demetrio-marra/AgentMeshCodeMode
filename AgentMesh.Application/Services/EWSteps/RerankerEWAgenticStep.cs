using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RerankerEWAgenticStep(
        RerankerAgent rerankerAgent,
        KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter) : IEWAgenticStep
    {
        public string Name => "Reranker";

        public string? AgentName => "Reranker";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(UserIntentParameter),
            typeof(ConversationTopicParameter),
            typeof(UserRequestedActionsParameter),
            typeof(UserProvidedDataParameter),
            typeof(UserPreferencesParameter),
            typeof(PastMemoriesQueryResultsParameter),
            typeof(KnowledgeBaseQueryResultsParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(KnowledgeBaseQueryResultsParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await rerankerAgent.ExecuteAsync(Values, cancellationToken);

            var knowledgeBaseResults = knowledgeBaseQueryResultsParameter.ValueAs(Values[typeof(KnowledgeBaseQueryResultsParameter)]) ?? [];
            var result = knowledgeBaseResults
                .Where(p => agentOutput.Result.Contains(p.File, StringComparer.OrdinalIgnoreCase))
                .ToList();

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(KnowledgeBaseQueryResultsParameter), result }
                }
            };
        }
    }
}
