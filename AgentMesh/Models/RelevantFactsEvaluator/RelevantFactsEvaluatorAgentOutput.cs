using AgentMesh.Models;

namespace AgentMesh.Models.RelevantFactsEvaluator
{
    public class RelevantFactsEvaluatorAgentOutput : IAgentOutput
    {
        public IEnumerable<string> RelevantUserMessages { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}

