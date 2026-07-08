using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.FunctionalAnalyst
{
    public class FunctionalAnalystAgentOutput : IAgentOutput
    {
        public string BusinessRequirements { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
