using AgentMesh.Utils;

namespace AgentMesh.Models.KnowledgeBase
{
    public class KnowledgeBaseGetDocsOutput
    {
        public IEnumerable<KnowledgeBaseGetDocsOutputItem> Results { get; set; } = [];

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Results", Results.Any() ? ListsFormatter.ToBulletList(Results.Select(r => r.File)) : "(No results found)" }
            };
        }
    }

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
