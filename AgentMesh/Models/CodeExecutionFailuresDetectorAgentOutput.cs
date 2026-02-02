namespace AgentMesh.Models
{
    public class CodeExecutionFailuresDetectorAgentOutput : IAgentOutput
    {
        public string Analysis { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
