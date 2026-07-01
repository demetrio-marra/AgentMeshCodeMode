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
            if (input == null)
            {
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));
            }

            var result = await _knowledgeBaseService.FindAsync(input, cancellationToken);

            return new KnowledgeBaseQueryResult
            {
                Results = result.Results.Select(r => new KnowledgeBaseQueryResultItem
                {
                    File = r.File,
                    Id = r.Id,
                    Relevance = r.Relevance,
                    Summary = r.Summary,
                    Title = r.Title
                }).ToList()
            };
        }
    }
}
