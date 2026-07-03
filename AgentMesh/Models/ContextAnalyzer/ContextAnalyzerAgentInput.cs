using AgentMesh.Models.AgentMemory;

namespace AgentMesh.Models.ContextAnalyzer
{
    public class ContextAnalyzerAgentInput
    {
        public string UserIntent { get; set; } = string.Empty;
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public IEnumerable<string> UserRequestDomains { get; set; } = [];
        public IEnumerable<string> ExtractedMemories { get; set; } = [];
        public IEnumerable<ExtractedKnowledgeItem> ExtractedKnowledgeBase { get; set; } = [];

        public class ExtractedKnowledgeItem
        {
            public string DocumentId { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string? Summary { get; set; }
        }
    }
}
