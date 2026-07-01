using AgentMesh.Application.Contracts;
using AgentMesh.Infrastructure.DocumentsCache.Models;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.DocumentsCache;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Infrastructure.DocumentsCache
{
    public class DummyDocumentsCacheService : IDocumentsCacheService
    {
        private readonly Dictionary<AgentMemoryCacheableQuery, HashSet<CacheableAgentMemoryQueryResultItem>> _agentMemoryCache = new();
        private readonly Dictionary<KnowledgeBaseCacheableQuery, HashSet<CacheableKnowledgeBaseQueryResultItem>> _knowledgeBaseCache = new();

        public async Task<Tuple<AgentMemoryQueryResult?, KnowledgeBaseQueryResult?>> ExecuteDocumentsCacheQueryAsync(
            IEnumerable<AgentMemoryCacheableQuery>? agentMemoryCachedQueries,
            IEnumerable<KnowledgeBaseCacheableQuery>? knowledgeBaseCachedQueries)
        {
            AgentMemoryQueryResult? agentMemoryResult = null;
            KnowledgeBaseQueryResult? knowledgeBaseResult = null;

            if (agentMemoryCachedQueries != null)
            {
                var agentMemoryResults = new HashSet<CacheableAgentMemoryQueryResultItem>();
                foreach (var query in agentMemoryCachedQueries)
                {
                    if (_agentMemoryCache.TryGetValue(query, out var cachedAgentMemoryItems))
                    {
                        agentMemoryResults.UnionWith(cachedAgentMemoryItems);
                    }
                }

                if (agentMemoryResults.Count > 0)
                {
                    agentMemoryResult = new AgentMemoryQueryResult { Results = agentMemoryResults };
                }
            }

            if (knowledgeBaseCachedQueries != null)
            {
                var knowledgeBaseResults = new HashSet<CacheableKnowledgeBaseQueryResultItem>();
                foreach (var query in knowledgeBaseCachedQueries)
                {
                    if (_knowledgeBaseCache.TryGetValue(query, out var cachedKnowledgeBaseItems))
                    {
                        knowledgeBaseResults.UnionWith(cachedKnowledgeBaseItems);
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
                    var newItems = agentMemoryQueryResults.Results
                        .Select(result => new CacheableAgentMemoryQueryResultItem(result));

                    if (_agentMemoryCache.TryGetValue(query, out var cachedItems))
                    {
                        cachedItems.UnionWith(newItems);
                    }
                    else
                    {
                        _agentMemoryCache[query] = newItems.ToHashSet();
                    }
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
                    var newItems = knowledgeBaseQueryResults.Results
                        .Select(result => new CacheableKnowledgeBaseQueryResultItem(result));

                    if (_knowledgeBaseCache.TryGetValue(query, out var cachedItems))
                    {
                        cachedItems.UnionWith(newItems);
                    }
                    else
                    {
                        _knowledgeBaseCache[query] = newItems.ToHashSet();
                    }
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
