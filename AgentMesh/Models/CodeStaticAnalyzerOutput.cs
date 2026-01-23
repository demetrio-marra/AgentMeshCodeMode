namespace AgentMesh.Models
{
    public class CodeStaticAnalyzerOutput : IAgentOutput
    {
        public IEnumerable<string> Violations { get; set; } = Array.Empty<string>();
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
