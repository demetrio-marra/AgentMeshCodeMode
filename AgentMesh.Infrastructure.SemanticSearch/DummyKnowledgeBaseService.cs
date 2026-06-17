using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;

namespace AgentMesh.Infrastructure.SemanticSearch
{
    public class DummyKnowledgeBaseService : IKnowledgeBaseService
    {
        Guid fakeGuid1 = Guid.Parse("65d94efd-6bd3-43bd-89e0-69295d6ce87f");
        Guid fakeGuid2 = Guid.Parse("db1d4b23-ff2d-4331-8b29-44de903ced30");

        public async Task<IEnumerable<KnowledgeBaseQueryResult>> KeywordsSearch(IEnumerable<string> searchTerms, CancellationToken cancellationToken = default)
        {
            var results = searchTerms.Select(term => new KnowledgeBaseQueryResult
            {
                SearchTerm = term,
                Id = fakeGuid1.ToString(),
                Title = $"GetSituazioneContabileCompleta API Description",
                Summary = $"Restituisce la situazione contabile completa per un singolo Cliente di Studio, riferita ad un singolo periodo contabile.",
                RelevanceScore = 1.0f
            }).DistinctBy(r => r.Id);
            return await Task.FromResult(results);
        }

        public async Task<IEnumerable<KnowledgeBaseQueryResult>> SemanticSearchAsync(IEnumerable<string> searchTerms, bool rerank = false, CancellationToken cancellationToken = default)
        {
            var results = searchTerms.Select(term => new KnowledgeBaseQueryResult
            {
                SearchTerm = term,
                Id = Guid.NewGuid().ToString(),
                Title = "GetUserByUsername",
                Summary = $"Restituisce i dati dell'utente dall'username",
                RelevanceScore = 0.5f
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
