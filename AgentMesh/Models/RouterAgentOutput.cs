namespace AgentMesh.Models
{
    public class RouterAgentOutput : IAgentOutput
    {
        public string Recipient { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
