using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class APIsKnowledgeBaseServiceSearchEWStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter,
        APISKnowledgeBaseQueryResultsParameter apisKnowledgeBaseQueryResultsParameter) : IEWStep
    {
        private const string APIsDocumentationCollectionName = "apis";

        public string Name => "APIs Knowledge Base Service Search";

        public bool IsAgentic => false;

        public string? AgentName => null;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var apiQueries = (domainsKnowledgeBaseQueryParameter.ParameterValue ?? []).ToList();

            var executorInput = new KnowledgeBaseQueryInput
            {
                Collections = [APIsDocumentationCollectionName],
                Queries = apiQueries
            };

            var executorOutput = await knowledgeBaseExecutor.QueryAsync(executorInput, cancellationToken);

            apisKnowledgeBaseQueryResultsParameter.ParameterValue = executorOutput.Results;

            return new EWStepResultRecord(null, null);
        }
    }
}
