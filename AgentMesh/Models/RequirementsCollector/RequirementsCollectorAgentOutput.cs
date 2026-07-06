using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.RequirementsCollector
{
    public class RequirementsCollectorAgentOutput : IAgentOutput
    {
        public IEnumerable<string> MissingPastMemories { get; set; } = [];
        public IEnumerable<KnowledgeBaseQueryInputItem> MissingKnowledgeBaseSearchEntries { get; set; } = [];

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }      
    }
}
