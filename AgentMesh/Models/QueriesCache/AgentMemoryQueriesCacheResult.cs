namespace AgentMesh.Models.QueriesCache
{
    public class AgentMemoryQueriesCacheResult
    {
        /// <summary>
        /// The total tokens used by the embedding service for generating the query embeddings.
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// The cached agent memory items found for the given queries.
        /// </summary>
        public IEnumerable<AgentMemoryQueriesCacheItem> Items { get; set; } = Enumerable.Empty<AgentMemoryQueriesCacheItem>();
    }
}
