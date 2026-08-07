using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainsKnowledgeBaseServiceSearchEWStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        private const string DomainsDocumentationCollectionName = "domains";

        public string Name => "Domains Knowledge Base Service Search";

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

            var searchQueries = typedDomainsQuery.ParameterValue ?? [];

            var executorInput = new KnowledgeBaseQueryInput
            {
                Collections = [DomainsDocumentationCollectionName],
                Queries = searchQueries
            };

            var executorOutput = await _knowledgeBaseExecutor.QueryAsync(executorInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.KnowledgeBaseQueryResults, executorOutput.Results);

            return new EWStepResultRecord(null, null);
        }
    }
}
