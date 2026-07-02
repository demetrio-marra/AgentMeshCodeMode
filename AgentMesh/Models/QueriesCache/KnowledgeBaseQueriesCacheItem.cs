using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.QueriesCache
{
    public class KnowledgeBaseQueriesCacheItem
    {
        public string Query { get; set; } = string.Empty;

        public KnowledgeBaseQuerySearchType QueryType { get; set; }

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
    }
}
