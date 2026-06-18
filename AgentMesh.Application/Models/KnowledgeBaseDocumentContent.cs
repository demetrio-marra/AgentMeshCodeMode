namespace AgentMesh.Application.Models
{
    /// <summary>
    /// This class represents the content of a knowledge base document, including the title, original file name, and the extracted relevant content.
    /// </summary>
    public class KnowledgeBaseDocumentContent
    {
        /// <summary>
        /// The title of the knowledge base entry.
        /// </summary>
        public string Title { get; set; } = string.Empty;

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
