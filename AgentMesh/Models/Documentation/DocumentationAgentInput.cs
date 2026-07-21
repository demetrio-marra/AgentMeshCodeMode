using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Models.Documentation
{
    public class DocumentationAgentInput
    {
        public StructuredUserRequest UserRequest { get; set; } = new();
        public IEnumerable<string> AgentMemories { get; set; } = [];
        public string KnowledgeBaseDocumentsContent { get; set; } = string.Empty;
        public string LanguageOfTheUser { get; set; } = string.Empty;
    }
}

