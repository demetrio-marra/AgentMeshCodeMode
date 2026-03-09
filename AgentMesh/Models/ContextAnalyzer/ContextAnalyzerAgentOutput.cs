namespace AgentMesh.Models.ContextAnalyzer
{
    public class ContextAnalyzerAgentOutput : IAgentOutput
    {
        public string EnrichedIntent { get; set; } = string.Empty;
        public IEnumerable<string> ActionableRequirements { get; set; } = Enumerable.Empty<string>();
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
 