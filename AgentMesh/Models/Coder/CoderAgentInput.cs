using AgentMesh.Utils;

namespace AgentMesh.Models.Coder
{
    public class CoderAgentInput
    {
        public string BusinessRequirements { get; set; } = string.Empty;

        public string TechnicalSpecification { get; set; } = string.Empty;

        /// <summary>
        /// Pre-fetched API documentation produced by the knowledge base service.
        /// Empty collection when no relevant documentation was found.
        /// </summary>
        public IEnumerable<AgentMesh.Models.KnowledgeBase.KnowledgeBaseGetDocsOutputItem> KnowledgeBaseAPIDocumentsContent { get; set; } = [];

        /// <summary>
        /// Selected API file locations from the Technical Analyst Agent.
        /// The Coder Agent should filter KnowledgeBaseAPIDocumentsContent to only include these files.
        /// </summary>
        public IEnumerable<string> SelectedAPIsFileLocations { get; set; } = [];

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Business requirements", BusinessRequirements },
                { "Technical specification", TechnicalSpecification },
                { "Knowledge Base API documents content", KnowledgeBaseAPIDocumentsContent.Any() ? ListsFormatter.ToBulletList(KnowledgeBaseAPIDocumentsContent.Select(f => f.File)) : "(No relevant documentation found)" },
                { "Selected API file locations", SelectedAPIsFileLocations.Any() ? ListsFormatter.ToBulletList(SelectedAPIsFileLocations) : "(No selected API files)" }
            };
        }
    }
}
