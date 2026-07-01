using AgentMesh.Application.Contracts;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.DocumentsCache;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Infrastructure.DocumentsCache
{
    public class DummyDocumentsCacheService : IDocumentsCacheService
    {
        private readonly Dictionary<AgentMemoryCacheableQuery, AgentMemoryQueryResult> _agentMemoryCache = new();
        private readonly Dictionary<KnowledgeBaseCacheableQuery, KnowledgeBaseQueryResult> _knowledgeBaseCache = new();

        public async Task<Tuple<AgentMemoryQueryResult?, KnowledgeBaseQueryResult?>> ExecuteDocumentsCacheQueryAsync(
            IEnumerable<AgentMemoryCacheableQuery>? agentMemoryCachedQueries,
            IEnumerable<KnowledgeBaseCacheableQuery>? knowledgeBaseCachedQueries)
        {
            AgentMemoryQueryResult? agentMemoryResult = null;
            KnowledgeBaseQueryResult? knowledgeBaseResult = null;

            if (agentMemoryCachedQueries != null)
            {
                var agentMemoryResults = new List<AgentMemoryQueryResultItem>();
                foreach (var query in agentMemoryCachedQueries)
                {
                    if (_agentMemoryCache.TryGetValue(query, out var cachedAgentMemory))
                    {
                        agentMemoryResults.AddRange(cachedAgentMemory.Results);
                    }
                }
                if (agentMemoryResults.Count > 0)
                {
                    agentMemoryResult = new AgentMemoryQueryResult { Results = agentMemoryResults };
                }
            }

            if (knowledgeBaseCachedQueries != null)
            {
                var knowledgeBaseResults = new List<KnowledgeBaseQueryResultItem>();
                foreach (var query in knowledgeBaseCachedQueries)
                {
                    if (_knowledgeBaseCache.TryGetValue(query, out var cachedKnowledgeBase))
                    {
                        knowledgeBaseResults.AddRange(cachedKnowledgeBase.Results);
                    }
                }
                if (knowledgeBaseResults.Count > 0)
                {
                    knowledgeBaseResult = new KnowledgeBaseQueryResult { Results = knowledgeBaseResults };
                }
            }

            return await Task.FromResult(new Tuple<AgentMemoryQueryResult?, KnowledgeBaseQueryResult?>(agentMemoryResult, knowledgeBaseResult));
        }

        public Task SaveAgentMemory(IEnumerable<AgentMemoryCacheableQuery>? agentMemoryCachedQueries, AgentMemoryQueryResult agentMemoryQueryResults)
        {
            if (agentMemoryCachedQueries != null && agentMemoryQueryResults != null)
            {
                foreach (var query in agentMemoryCachedQueries)
                {
                    _agentMemoryCache[query] = agentMemoryQueryResults;
                }
            }

            return Task.CompletedTask;
        }

        public Task SaveKnowledgeBase(IEnumerable<KnowledgeBaseCacheableQuery> knowledgeBaseCachedQueries, KnowledgeBaseQueryResult knowledgeBaseQueryResults)
        {
            if (knowledgeBaseCachedQueries != null && knowledgeBaseQueryResults != null)
            {
                foreach (var query in knowledgeBaseCachedQueries)
                {
                    _knowledgeBaseCache[query] = knowledgeBaseQueryResults;
                }
            }

            return Task.CompletedTask;
        }

        public Task<Tuple<IEnumerable<AgentMemoryCacheableQuery>, IEnumerable<KnowledgeBaseCacheableQuery>>> GetAllCachedSearchesAsync()
        {
            return Task.FromResult(new Tuple<IEnumerable<AgentMemoryCacheableQuery>, IEnumerable<KnowledgeBaseCacheableQuery>>(
                _agentMemoryCache.Keys,
                _knowledgeBaseCache.Keys));
        }
    }
}
