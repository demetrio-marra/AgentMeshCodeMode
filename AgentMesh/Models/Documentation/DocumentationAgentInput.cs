namespace AgentMesh.Models.Documentation
{
    public class DocumentationAgentInput
    {
        public string EnrichedUserRequest { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public Dictionary<string, IEnumerable<string>> Entities { get; set; } = new();
        public IEnumerable<string> UserPreferences { get; set; } = [];
        public IEnumerable<string> AgentMemories { get; set; } = [];
        public string KnowledgeBaseDocumentsContent { get; set; } = string.Empty;
    }
}

