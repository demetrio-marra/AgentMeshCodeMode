namespace AgentMesh.Models
{
    public class ContextManagerAgentOutput : IAgentOutput
    {
        public string ResponseText { get; set; } = string.Empty;
        public bool EngageBusinessAnalyst { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
