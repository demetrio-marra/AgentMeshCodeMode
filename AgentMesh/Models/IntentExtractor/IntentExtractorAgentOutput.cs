namespace AgentMesh.Models.IntentExtractor
{
    public class IntentExtractorAgentOutput : IAgentOutput
    {
        public string Query { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
