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
        /// <returns>The cached items found for the given queries.</returns>
        Task<IEnumerable<AgentMemoryQueriesCacheItemOutput>> GetCachedItemsAsync(IEnumerable<AgentMemoryQueriesCacheItemInput> queries);

        /// <summary>
        /// Sets cached agent memory items.
        /// </summary>
        /// <param name="cachedItems">The cached items to store.</param>
        Task SetCachedItemsAsync(IEnumerable<AgentMemoryQueriesCacheItemOutput> cachedItems);

        /// <summary>
        /// Gets cached knowledge base items for the given queries.
        /// </summary>
        /// <param name="queries">The knowledge base queries to get cached items for.</param>
        /// <returns>The cached items found for the given knowledge base queries.</returns>
        Task<IEnumerable<KnowledgeBaseQueriesCacheItemOutput>> GetKnowledgeBaseCachedItemsAsync(IEnumerable<KnowledgeBaseQueriesCacheItemInput> queries);

        /// <summary>
        /// Sets cached knowledge base items.
        /// </summary>
        /// <param name="cachedItems">The cached knowledge base items to store.</param>
        Task SetKnowledgeBaseCachedItemsAsync(IEnumerable<KnowledgeBaseQueriesCacheItemOutput> cachedItems);
    }
}
