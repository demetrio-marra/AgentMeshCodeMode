using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class APIsKnowledgeBaseServiceSearchEWStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        private const string APIsDocumentationCollectionName = "apis";

        public string Name => "APIs Knowledge Base Service Search";

        public bool IsAgentic => false;

        public string? AgentName => null;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.DomainsKnowledgeBaseQuery
        ];

        private readonly KnowledgeBaseExecutor _knowledgeBaseExecutor = knowledgeBaseExecutor;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var domainsQueryParameter = inputParameters.Single(p => p.Name == EWParameterNames.DomainsKnowledgeBaseQuery);
            if (domainsQueryParameter is not DomainsKnowledgeBaseQueryParameter typedDomainsQuery)
                throw new InvalidOperationException($"Parameter {EWParameterNames.DomainsKnowledgeBaseQuery} is not of type DomainsKnowledgeBaseQueryParameter");

            var apiQueries = (typedDomainsQuery.ParameterValue ?? []).ToList();

            var executorInput = new KnowledgeBaseQueryInput
            {
                Collections = [APIsDocumentationCollectionName],
                Queries = apiQueries
            };

            var executorOutput = await _knowledgeBaseExecutor.QueryAsync(executorInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.APISKnowledgeBaseQueryResults, executorOutput.Results);

            return new EWStepResultRecord(null, null);
        }
    }
}
