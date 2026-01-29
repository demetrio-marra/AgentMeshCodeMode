namespace AgentMesh.Models
{
    public class ConversationSummarizerAgentOutput : IAgentOutput
    {
        public string Summary { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
