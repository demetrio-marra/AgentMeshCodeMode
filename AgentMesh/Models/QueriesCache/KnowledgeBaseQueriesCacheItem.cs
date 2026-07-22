namespace AgentMesh.Models.QueriesCache
{
    public class KnowledgeBaseQueriesCacheItem
    {
        public string FoundQuery { get; set; } = string.Empty;

        public string FoundQueryType { get; set; } = string.Empty;

        public string SearchedQuery { get; set; } = string.Empty;

        public string SearchedQueryType { get; set; } = string.Empty;
        /// <summary>
        /// The unique identifier of the knowledge base entry.
        /// </summary>
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>
        /// The title of the knowledge base entry.
        /// </summary>
        public string DocumentTitle { get; set; } = string.Empty;

        /// <summary>
        /// A brief summary or description of the knowledge base entry.
        /// </summary>
        public string? DocumentSummary { get; set; }

        /// <summary>
        /// The original documentation file name associated with the knowledge base entry, if available.
        /// </summary>
        public string DocumentFile { get; set; } = string.Empty;

        public double Relevance { get; set; }
    }
}
