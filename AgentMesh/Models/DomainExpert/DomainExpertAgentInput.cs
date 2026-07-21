namespace AgentMesh.Models.DomainExpert
{
    public class DomainExpertAgentInput
    {
        public string Intent { get; set; } = string.Empty;
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public Dictionary<string, IEnumerable<string>> Entities { get; set; } = new();
        public IEnumerable<string> UserPreferences { get; set; } = [];
        public IEnumerable<string> AgentMemories { get; set; } = [];
        public string KnowledgeBaseDocumentsContent { get; set; } = string.Empty;
        public string DataToComment { get; set; } = string.Empty;
        public string LanguageOfTheUser { get; set; } = string.Empty;
    }
}
