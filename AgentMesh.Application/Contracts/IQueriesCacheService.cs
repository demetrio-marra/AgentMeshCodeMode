using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.QueriesCache;
using AgentMesh.Models.QueriesCache;

namespace AgentMesh.Application.Contracts
{
    /// <summary>
    /// Implements methods to read and write AgentMemory and KnowledgeBase cached items to a cache storage.
    /// </summary>
    public interface IQueriesCacheService
    {
        /// <summary>
        /// Gets cached agent memory items for the given queries.
        /// </summary>
        /// <param name="queries">The queries to get cached items for.</param>
        /// <returns>The cached items found for the given queries, including token usage information from the embedding service.</returns>
        Task<AgentMemoryQueriesCacheResult> GetMemoryCachedItemsAsync(IEnumerable<AgentMemoryQueriesCacheItemInput> queries);

        /// <summary>
        /// Sets cached agent memory items.
        /// </summary>
        /// <param name="cachedItems">The cached items to store.</param>
        /// <returns>Token usage information from the embedding service during the update operation.</returns>
        Task<QueryCacheUpdateResult> SetMemoryCachedItemsAsync(IEnumerable<AgentMemoryQueriesCacheItem> cachedItems);

        /// <summary>
        /// Gets cached knowledge base items for the given queries.
        /// </summary>
        /// <param name="queries">The knowledge base queries to get cached items for.</param>
        /// <returns>The cached items found for the given knowledge base queries, including token usage information from the embedding service.</returns>
        Task<KnowledgeBaseQueriesCacheResult> GetKnowledgeBaseCachedItemsAsync(IEnumerable<KnowledgeBaseQueryInputItem> queries);

        /// <summary>
        /// Sets cached knowledge base items.
        /// </summary>
        /// <param name="cachedItems">The cached knowledge base items to store.</param>
        /// <returns>Token usage information from the embedding service during the update operation.</returns>
        Task<QueryCacheUpdateResult> SetKnowledgeBaseCachedItemsAsync(IEnumerable<KnowledgeBaseQueriesCacheItem> cachedItems);
    }
}
