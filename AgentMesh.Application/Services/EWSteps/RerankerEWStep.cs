using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Reranker;
using AgentMesh.Application.Models.Workflows.Parameters;
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

        public string? AgentName => RerankerAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly RerankerAgent rerankerAgent = rerankerAgent;
        private readonly UserIntentParameter userIntentParameter = userIntentParameter;
        private readonly KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter = knowledgeBaseQueryResultsParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var candidates = (this.knowledgeBaseQueryResultsParameter.ParameterValue ?? []).ToList();
            if (candidates.Count == 0)
            {
                return new EWStepResultRecord(null, null);
            }

            var sr = new StructuredUserRequest
            {
                Intent = this.userIntentParameter.ParameterValue ?? string.Empty
            };

            var agentInput = new RerankerAgentInput
            {
                StructuredUserRequest = sr,
                QueryResults = candidates
            };

            var agentOutput = await this.rerankerAgent.ExecuteAsync(agentInput, cancellationToken);

            this.knowledgeBaseQueryResultsParameter.ParameterValue = agentOutput.QueryResults;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
