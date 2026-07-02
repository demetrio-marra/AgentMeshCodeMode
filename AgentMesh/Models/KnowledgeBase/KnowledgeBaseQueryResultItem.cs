namespace AgentMesh.Models.KnowledgeBase
{
    public class KnowledgeBaseQueryResultItem
    {
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
        public string? Summary { get; set; }

        /// <summary>
        /// The original documentation file name associated with the knowledge base entry, if available.
        /// </summary>
        public string File { get; set; } = string.Empty;

        /// <summary>
        /// The relevance score of the knowledge base entry in relation to the search query, if available.
        /// </summary>
        public double? Relevance { get; set; }
    }
}
