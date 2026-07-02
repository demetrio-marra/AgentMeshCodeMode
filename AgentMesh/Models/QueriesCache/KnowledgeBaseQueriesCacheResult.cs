namespace AgentMesh.Models.QueriesCache
{
    public class KnowledgeBaseQueriesCacheResult
    {
        /// <summary>
        /// The total tokens used by the embedding service for generating the query embeddings.
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// The cached knowledge base items found for the given queries.
        /// </summary>
        public IEnumerable<KnowledgeBaseQueriesCacheItem> Items { get; set; } = Enumerable.Empty<KnowledgeBaseQueriesCacheItem>();
    }
}
