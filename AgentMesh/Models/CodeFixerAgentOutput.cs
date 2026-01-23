namespace AgentMesh.Models
{
    public class CodeFixerAgentOutput : IAgentOutput
    {
        public string FixedCode { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
