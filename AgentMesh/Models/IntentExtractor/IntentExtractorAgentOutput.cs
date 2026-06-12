namespace AgentMesh.Models.IntentExtractor
{
    public class IntentExtractorAgentOutput : IAgentOutput
    {
        public string UserIntent { get; set; } = string.Empty;
        public IEnumerable<string> MissingPastMemories { get; set; } = [];
        public IEnumerable<string> MissingKnowledgeBaseEntries { get; set; } = [];

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
