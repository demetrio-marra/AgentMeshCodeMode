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
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Reranker";

        public bool IsAgentic => true;

        public string? AgentName => RerankerAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.UserIntent,
            EWParameterNames.KnowledgeBaseQueryResults
        ];

        private readonly RerankerAgent _rerankerAgent = rerankerAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var intentParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserIntent);
            if (intentParameter is not UserIntentParameter typedIntent)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserIntent} is not of type UserIntentParameter");

            var queryResultsParameter = inputParameters.Single(p => p.Name == EWParameterNames.KnowledgeBaseQueryResults);
            if (queryResultsParameter is not KnowledgeBaseQueryResultsParameter typedQueryResults)
                throw new InvalidOperationException($"Parameter {EWParameterNames.KnowledgeBaseQueryResults} is not of type KnowledgeBaseQueryResultsParameter");

            var candidates = (typedQueryResults.ParameterValue ?? []).ToList();
            if (candidates.Count == 0)
            {
                return new EWStepResultRecord(null, null);
            }

            var sr = new StructuredUserRequest
            {
                Intent = typedIntent.ParameterValue ?? string.Empty
            };

            var agentInput = new RerankerAgentInput
            {
                StructuredUserRequest = sr,
                QueryResults = candidates
            };

            var agentOutput = await _rerankerAgent.ExecuteAsync(agentInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.KnowledgeBaseQueryResults, agentOutput.QueryResults);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
