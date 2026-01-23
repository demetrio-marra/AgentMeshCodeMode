namespace AgentMesh.Models
{
    public class CoderAgentOutput : IAgentOutput
    {
        public string CodeToRun { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
