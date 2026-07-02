namespace AgentMesh.Models.CodeStaticAnalyzer
{
    public class CodeStaticAnalyzerOutput : IAgentOutput
    {
        public IEnumerable<string> Violations { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
