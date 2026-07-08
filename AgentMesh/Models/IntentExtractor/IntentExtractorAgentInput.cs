namespace AgentMesh.Models.IntentExtractor
{
    public class IntentExtractorAgentInput
    {
        public List<ContextMessage> ContextMessages { get; set; } = [];
        public string UserLastRequest { get; set; } = string.Empty;
        public IEnumerable<string> ApplicationDomainList { get; set; } = [];
        public string LanguageOfKnowledgeBase { get; set; } = string.Empty;
    }
}
