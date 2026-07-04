using AgentMesh.Application.Contracts;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class KnowledgeBaseSearchFastExecutor(IKnowledgeBaseService knowledgeBaseService) : IKnowledgeBaseSearchFastExecutor
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService = knowledgeBaseService;

        async Task<KnowledgeBaseQueryResult> IExecutor<KnowledgeBaseQueryInput, KnowledgeBaseQueryResult>.ExecuteAsync(KnowledgeBaseQueryInput input, CancellationToken cancellationToken)
        {
            if (input == null)
            {
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));
            }

            var lexQueries = input.Queries.Where(p => p.SearchType == KnowledgeBaseQuerySearchType.Keyword)
                .Select(q => q.Query)
                .ToList();

            var result = await _knowledgeBaseService.KeywordsSearch(lexQueries, input.Collections, false, cancellationToken);

            return new KnowledgeBaseQueryResult
            {
                Results = [.. result.Results.Select(r => new KnowledgeBaseQueryResultItem
                {
                    File = r.File,
                    Id = r.Id,
                    Relevance = r.Relevance,
                    Summary = r.Summary,
                    Title = r.Title
                })]
            };
        }
    }
}
