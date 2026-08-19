using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class APIsKnowledgeBaseServiceSearchEWCodeStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter) : IEWStep
    {
        private const string APIsDocumentationCollectionName = "apis";

        public string Name => "APIs Knowledge Base Service Search";

        public IEnumerable<Type> InputParameterTypes => [typeof(DomainsKnowledgeBaseQueryParameter)];

        public IEnumerable<Type> OutputParameterTypes => [typeof(APISKnowledgeBaseQueryResultsParameter)];


        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var apiQueries = domainsKnowledgeBaseQueryParameter.ValueAs(Values[typeof(DomainsKnowledgeBaseQueryParameter)]) ?? [];

            var executorInput = new KnowledgeBaseQueryInput
            {
                Collections = [APIsDocumentationCollectionName],
                Queries = apiQueries
            };

            var executorOutput = await knowledgeBaseExecutor.QueryAsync(executorInput, cancellationToken);

            return new EWStepExecutionResult
            {
                OutputMutations = new Dictionary<Type, object?>
                {
                    [typeof(APISKnowledgeBaseQueryResultsParameter)] = executorOutput.Results
                }
            };
        }
    }
}
