using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.Coder
{
    public class CoderAgentInput
    {
        public string BusinessRequirements { get; set; } = string.Empty;

        /// <summary>
        /// Pre-fetched API documentation produced by the knowledge base service.
        /// Empty collection when no relevant documentation was found.
        /// </summary>
        public IEnumerable<AgentMesh.Models.KnowledgeBase.KnowledgeBaseGetDocsOutputItem> KnowledgeBaseAPIDocumentsContent { get; set; } = [];
    }
}
