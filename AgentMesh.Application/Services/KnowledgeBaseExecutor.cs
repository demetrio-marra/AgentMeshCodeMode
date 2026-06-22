using AgentMesh.Application.Contracts;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class KnowledgeBaseExecutor : IKnowledgeBaseSearchExecutor, IKnowledgeBaseGetDocsExecutor
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService;


        public KnowledgeBaseExecutor(IKnowledgeBaseService knowledgeBaseService)
        {
            _knowledgeBaseService = knowledgeBaseService;
        }


        async Task<KnowledgeBaseGetDocsOutput> IExecutor<KnowledgeBaseGetDocsInput, KnowledgeBaseGetDocsOutput>.ExecuteAsync(KnowledgeBaseGetDocsInput input, CancellationToken cancellationToken)
        {
            if (input.FilePaths == null || !input.FilePaths.Any())
            {
                throw new ArgumentException("FilePaths cannot be null or empty.", nameof(input.FilePaths));
            }

            var results = await _knowledgeBaseService.GetKnowledgeBaseEntriesContentAsync(input.FilePaths, cancellationToken);
            return new KnowledgeBaseGetDocsOutput { Results = results.Select(r => new KnowledgeBaseGetDocsOutputItem { File = r.File, Content = r.Content }).ToList() };
        }


        async Task<KnowledgeBaseQueryResult> IExecutor<KnowledgeBaseQueryInput, KnowledgeBaseQueryResult>.ExecuteAsync(KnowledgeBaseQueryInput input, CancellationToken cancellationToken)
        {
            if (input.Queries == null || !input.Queries.Any())
            {
                throw new ArgumentException("Queries cannot be null or empty.", nameof(input.Queries));
            }

            var result = Enumerable.Empty<Application.Models.KnowledgeBaseQueryResult>();
            if (input.SearchType == KnowledgeBaseQuerySearchType.KeywordsOnly)
            {
                result = await _knowledgeBaseService.KeywordsSearch(input.Queries, input.Collections, false, cancellationToken);
            }
            else if (input.SearchType == KnowledgeBaseQuerySearchType.SemanticOnly)
            {
                result = await _knowledgeBaseService.SemanticSearchAsync(input.Queries, input.Collections, true, cancellationToken);
            }
            else if (input.SearchType == KnowledgeBaseQuerySearchType.Full)
            {
                result = await _knowledgeBaseService.FindAsync(input.Queries, input.Collections, cancellationToken);
            }
            
            return new KnowledgeBaseQueryResult { Results = result.Select(r => new KnowledgeBaseQueryResultItem
            {
                File = r.File,
                Id = r.Id,
                Relevance = r.Relevance,
                Summary = r.Summary,
                Title = r.Title
            }).ToList() };
        }
    }
}
