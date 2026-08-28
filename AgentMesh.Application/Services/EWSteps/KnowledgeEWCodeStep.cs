using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class KnowledgeEWCodeStep(
        IKnowledgeService knowledgeService,
        KnowledgeQueryParameter knowledgeQueryParameter) : IEWStep
    {
        public string Name => "Knowledge Service Search";

        public IEnumerable<Type> InputParameterTypes => [typeof(KnowledgeQueryParameter)];
        public IEnumerable<Type> OutputParameterTypes => [typeof(KnowledgeQueryResultParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
            //var knowledgeQueryResult = await knowledgeService.QueryKnowledgeAsync(knowledgeQueryParameter.ValueAs(Values[typeof(KnowledgeQueryParameter)]), cancellationToken);

            //var executorInput = new KnowledgeBaseQueryInput
            //{
            //    Collections = [DomainsDocumentationCollectionName],
            //    Queries = searchQueries
            //};

            //var executorOutput = await knowledgeBaseExecutor.QueryAsync(executorInput, cancellationToken);

            //return new EWStepExecutionResult
            //{
            //    OutputMutations = new Dictionary<Type, object?>
            //    {
            //        { typeof(KnowledgeBaseQueryResultsParameter), executorOutput.Results }
            //    }
            //};
        }
    }
}
