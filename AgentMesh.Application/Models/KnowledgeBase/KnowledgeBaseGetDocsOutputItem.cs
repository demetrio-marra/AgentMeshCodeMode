namespace AgentMesh.Application.Models.KnowledgeBase
{
    public class KnowledgeBaseGetDocsOutputItem
    {
        /// <summary>
        /// The original documentation file name associated with the knowledge base entry, if available.
        /// </summary>
        public string? File { get; set; }

        /// <summary>
        /// The full content of the knowledge base entry, which may include the extracted relevant sections from the original documentation file.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}