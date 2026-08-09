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
        DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter,
        KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter) : IEWStep
    {
        private const string DomainsDocumentationCollectionName = "domains";

        public string Name => "Domains Knowledge Base Service Search";

        public bool IsAgentic => false;

        public string? AgentName => null;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly KnowledgeBaseExecutor knowledgeBaseExecutor = knowledgeBaseExecutor;
        private readonly DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter = domainsKnowledgeBaseQueryParameter;
        private readonly KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter = knowledgeBaseQueryResultsParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var searchQueries = this.domainsKnowledgeBaseQueryParameter.ParameterValue ?? [];

            var executorInput = new KnowledgeBaseQueryInput
            {
                Collections = [DomainsDocumentationCollectionName],
                Queries = searchQueries
            };

            var executorOutput = await this.knowledgeBaseExecutor.QueryAsync(executorInput, cancellationToken);

            this.knowledgeBaseQueryResultsParameter.ParameterValue = executorOutput.Results;

            return new EWStepResultRecord(null, null);
        }
    }
}
