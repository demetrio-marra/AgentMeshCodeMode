namespace AgentMesh.Models
{
    public class ContextManagerAgentOutput : IAgentOutput
    {
        public string ContextEnrichedUserSentenceText { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
