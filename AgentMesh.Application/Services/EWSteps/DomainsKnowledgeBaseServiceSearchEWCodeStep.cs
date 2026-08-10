using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainsKnowledgeBaseServiceSearchEWCodeStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter,
        KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter) : IEWCodeStep
    {
        private const string DomainsDocumentationCollectionName = "domains";

        public string Name => "Domains Knowledge Base Service Search";

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var searchQueries = domainsKnowledgeBaseQueryParameter.ParameterValue ?? [];

            var executorInput = new KnowledgeBaseQueryInput
            {
                Collections = [DomainsDocumentationCollectionName],
                Queries = searchQueries
            };

            var executorOutput = await knowledgeBaseExecutor.QueryAsync(executorInput, cancellationToken);

            knowledgeBaseQueryResultsParameter.ParameterValue = executorOutput.Results;
        }
    }
}
