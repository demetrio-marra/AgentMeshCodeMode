namespace AgentMesh.Application.Models
{
    public class KnowledgeBaseQueryResult
    {
        /// <summary>
        /// Gets or sets the search term used to filter or query results.
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier of the knowledge base entry.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// The title of the knowledge base entry.
        /// </summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// A brief summary or description of the knowledge base entry.
        /// </summary>
        public string Summary { get; set; } = string.Empty;
        /// <summary>
        /// The relevance score of the knowledge base entry with respect to the query, typically in the range [0, 1].
        /// </summary>
        public float RelevanceScore { get; set; }
    }
}
