using AgentMesh.Application.Models.RequestAnalysis;
using AgentMesh.Application.Models.Reranker;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RerankerEWAgenticStep(
        RerankerAgent rerankerAgent,
        UserIntentParameter userIntentParameter,
        KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter) : IEWAgenticStep
    {
        public string Name => "Reranker";

        public string? AgentName => "Reranker";

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => false;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var candidates = (knowledgeBaseQueryResultsParameter.ParameterValue ?? []).ToList();
            if (candidates.Count == 0)
            {
                return new EWAgenticStepResultRecord(null, null);
            }

            var sr = new StructuredUserRequest
            {
                Intent = userIntentParameter.ParameterValue ?? string.Empty
            };

            var agentInput = new RerankerAgentInput
            {
                StructuredUserRequest = sr,
                QueryResults = candidates
            };

            var agentOutput = await rerankerAgent.ExecuteAsync(agentInput, cancellationToken);

            knowledgeBaseQueryResultsParameter.ParameterValue = agentOutput.QueryResults;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
