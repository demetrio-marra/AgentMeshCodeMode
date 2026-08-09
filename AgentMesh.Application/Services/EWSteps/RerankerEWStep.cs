using AgentMesh.Application.Models.Reranker;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RerankerEWStep(
        RerankerAgent rerankerAgent,
        UserIntentParameter userIntentParameter,
        KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter) : IEWStep
    {
        public string Name => "Reranker";

        public bool IsAgentic => true;

        public string? AgentName => "Reranker";

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var candidates = (knowledgeBaseQueryResultsParameter.ParameterValue ?? []).ToList();
            if (candidates.Count == 0)
            {
                return new EWStepResultRecord(null, null);
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

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
