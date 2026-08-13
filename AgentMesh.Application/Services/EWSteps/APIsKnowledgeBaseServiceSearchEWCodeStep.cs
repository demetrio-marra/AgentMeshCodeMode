using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class APIsKnowledgeBaseServiceSearchEWCodeStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter,
        APISKnowledgeBaseQueryResultsParameter apisKnowledgeBaseQueryResultsParameter) : IEWCodeStep
    {
        private const string APIsDocumentationCollectionName = "apis";

        public string Name => "APIs Knowledge Base Service Search";
        
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var apiQueries = (domainsKnowledgeBaseQueryParameter.ParameterValue ?? []).ToList();

            var executorInput = new KnowledgeBaseQueryInput
            {
                Collections = [APIsDocumentationCollectionName],
                Queries = apiQueries
            };

            var executorOutput = await knowledgeBaseExecutor.QueryAsync(executorInput, cancellationToken);

            apisKnowledgeBaseQueryResultsParameter.ParameterValue = executorOutput.Results;
        }
    }
}
