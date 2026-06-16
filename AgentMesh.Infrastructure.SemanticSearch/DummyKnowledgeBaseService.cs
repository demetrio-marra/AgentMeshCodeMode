using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;

namespace AgentMesh.Infrastructure.SemanticSearch
{
    public class DummyKnowledgeBaseService : IKnowledgeBaseService
    {
        public async Task<IEnumerable<KnowledgeBaseQueryResult>> ExactSearchAsync(IEnumerable<string> searchTerms, CancellationToken cancellationToken = default)
        {
            var results = searchTerms.Select(term => new KnowledgeBaseQueryResult
            {
                SearchTerm = term,
                Id = Guid.NewGuid().ToString(),
                Title = $"Exact match for '{term}'",
                Summary = $"This is a dummy exact search result for the term '{term}'.",
                RelevanceScore = 1.0
            });
            return await Task.FromResult(results);
        }
        public async Task<IEnumerable<KnowledgeBaseQueryResult>> SemanticSearchAsync(IEnumerable<string> searchTerms, bool rerank = false, CancellationToken cancellationToken = default)
        {
            var results = searchTerms.Select(term => new KnowledgeBaseQueryResult
            {
                SearchTerm = term,
                Id = Guid.NewGuid().ToString(),
                Title = $"Semantic match for '{term}'",
                Summary = $"This is a dummy semantic search result for the term '{term}'.",
                RelevanceScore = 0.5
            });
            return await Task.FromResult(results);
        }
    }
}
