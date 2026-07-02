using AgentMesh.Application.Contracts;
using AgentMesh.Infrastructure.DocumentsCache.Configuration;
using AgentMesh.Infrastructure.DocumentsCache.Models;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.DocumentsCache;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Infrastructure.DocumentsCache
{
    public class DummyDocumentsCacheService : IDocumentsCacheService
    {
        private readonly Dictionary<AgentMemoryCacheableQuery, CacheEntry<CacheableAgentMemoryQueryResultItem>> _agentMemoryCache = [];
        private readonly Dictionary<KnowledgeBaseCacheableQuery, CacheEntry<CacheableKnowledgeBaseQueryResultItem>> _knowledgeBaseCache = [];
        private readonly TimeSpan? _cacheExpiration;

        public DummyDocumentsCacheService(DocumentsCacheServiceConfiguration configuration)
        {
            if (configuration.ExpirationMinutes > 0)
            {
                _cacheExpiration = TimeSpan.FromMinutes(configuration.ExpirationMinutes);
            }
        }

        public async Task<Tuple<AgentMemoryQueryResult?, KnowledgeBaseQueryResult?>> ExecuteDocumentsCacheQueryAsync(
            IEnumerable<AgentMemoryCacheableQuery>? agentMemoryCachedQueries,
            IEnumerable<KnowledgeBaseCacheableQuery>? knowledgeBaseCachedQueries)
        {
            EvictExpiredEntries();

            AgentMemoryQueryResult? agentMemoryResult = null;
            KnowledgeBaseQueryResult? knowledgeBaseResult = null;

            if (agentMemoryCachedQueries != null)
            {
                var agentMemoryResults = new HashSet<CacheableAgentMemoryQueryResultItem>();
                foreach (var query in agentMemoryCachedQueries)
                {
                    if (_agentMemoryCache.TryGetValue(query, out var cacheEntry))
                    {
                        agentMemoryResults.UnionWith(cacheEntry.Items);
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
                    if (_knowledgeBaseCache.TryGetValue(query, out var cacheEntry))
                    {
                        knowledgeBaseResults.UnionWith(cacheEntry.Items);
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
            EvictExpiredEntries();

            if (agentMemoryCachedQueries != null && agentMemoryQueryResults != null)
            {
                foreach (var query in agentMemoryCachedQueries)
                {
                    var newItems = agentMemoryQueryResults.Results
                        .Select(result => new CacheableAgentMemoryQueryResultItem(result));

                    if (_agentMemoryCache.TryGetValue(query, out var cacheEntry))
                    {
                        cacheEntry.Items.UnionWith(newItems);
                        cacheEntry.ExpiresAtUtc = GetExpirationTime();
                    }
                    else
                    {
                        _agentMemoryCache[query] = new CacheEntry<CacheableAgentMemoryQueryResultItem>([.. newItems], GetExpirationTime());
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task SaveKnowledgeBase(IEnumerable<KnowledgeBaseCacheableQuery> knowledgeBaseCachedQueries, KnowledgeBaseQueryResult knowledgeBaseQueryResults)
        {
            EvictExpiredEntries();

            if (knowledgeBaseCachedQueries != null && knowledgeBaseQueryResults != null)
            {
                foreach (var query in knowledgeBaseCachedQueries)
                {
                    var newItems = knowledgeBaseQueryResults.Results
                        .Select(result => new CacheableKnowledgeBaseQueryResultItem(result));

                    if (_knowledgeBaseCache.TryGetValue(query, out var cacheEntry))
                    {
                        cacheEntry.Items.UnionWith(newItems);
                        cacheEntry.ExpiresAtUtc = GetExpirationTime();
                    }
                    else
                    {
                        _knowledgeBaseCache[query] = new CacheEntry<CacheableKnowledgeBaseQueryResultItem>([.. newItems], GetExpirationTime());
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task<Tuple<IEnumerable<AgentMemoryCacheableQuery>, IEnumerable<KnowledgeBaseCacheableQuery>>> GetAllCachedSearchesAsync()
        {
            EvictExpiredEntries();

            return Task.FromResult(new Tuple<IEnumerable<AgentMemoryCacheableQuery>, IEnumerable<KnowledgeBaseCacheableQuery>>(
                _agentMemoryCache.Keys,
                _knowledgeBaseCache.Keys));
        }

        private DateTime? GetExpirationTime()
        {
            return _cacheExpiration.HasValue
                ? DateTime.UtcNow.Add(_cacheExpiration.Value)
                : null;
        }

        private void EvictExpiredEntries()
        {
            var now = DateTime.UtcNow;

            var expiredAgentMemoryQueries = _agentMemoryCache
                .Where(entry => IsExpired(entry.Value, now))
                .Select(entry => entry.Key)
                .ToList();

            foreach (var query in expiredAgentMemoryQueries)
            {
                _agentMemoryCache.Remove(query);
            }

            var expiredKnowledgeBaseQueries = _knowledgeBaseCache
                .Where(entry => IsExpired(entry.Value, now))
                .Select(entry => entry.Key)
                .ToList();

            foreach (var query in expiredKnowledgeBaseQueries)
            {
                _knowledgeBaseCache.Remove(query);
            }
        }

        private static bool IsExpired<TItem>(CacheEntry<TItem> entry, DateTime now)
        {
            return entry.ExpiresAtUtc.HasValue && entry.ExpiresAtUtc.Value <= now;
        }

        private sealed class CacheEntry<TItem>(HashSet<TItem> items, DateTime? expiresAtUtc)
        {
            public HashSet<TItem> Items { get; } = items;
            public DateTime? ExpiresAtUtc { get; set; } = expiresAtUtc;
        }
    }
}
