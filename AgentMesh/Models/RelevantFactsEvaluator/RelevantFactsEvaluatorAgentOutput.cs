namespace AgentMesh.Models.RelevantFactsEvaluator
{
    public class RelevantFactsEvaluatorAgentOutput : IAgentOutput
    {
        public bool IsWorthSaving { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
