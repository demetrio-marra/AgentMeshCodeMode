using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainsKnowledgeBaseServiceSearchEWCodeStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter) : IEWStep
    {
        private const string DomainsDocumentationCollectionName = "domains";

        public string Name => "Domains Knowledge Base Service Search";

        public IEnumerable<Type> InputParameterTypes => [typeof(DomainsKnowledgeBaseQueryParameter)];

        public IEnumerable<Type> OutputParameterTypes => [typeof(KnowledgeBaseQueryResultsParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var searchQueries = domainsKnowledgeBaseQueryParameter.ValueAs(Values[typeof(DomainsKnowledgeBaseQueryParameter)]) ?? [];

            var executorInput = new KnowledgeBaseQueryInput
            {
                Collections = [DomainsDocumentationCollectionName],
                Queries = searchQueries
            };

            var executorOutput = await knowledgeBaseExecutor.QueryAsync(executorInput, cancellationToken);

            return new EWStepExecutionResult
            {
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(KnowledgeBaseQueryResultsParameter), executorOutput.Results }
                }
            };
        }
    }
}
