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

        public async Task<string> GetKnowledgeBaseEntryContentAsync(string id, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult($"This is the content for knowledge base entry with ID '{id}'.");
        }

        public async Task<IDictionary<string, string?>> GetKnowledgeBaseEntriesContentAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            var result = ids.ToDictionary(
                id => id,
                id => (string?)$"This is the content for knowledge base entry with ID '{id}'.");
            return await Task.FromResult(result);
        }
    }
}
