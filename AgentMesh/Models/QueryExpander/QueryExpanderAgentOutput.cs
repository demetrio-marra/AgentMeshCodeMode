using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.QueryExpander
{
    public class QueryExpanderAgentOutput : IAgentOutput
    {
        public IEnumerable<KnowledgeBaseQueryInputItem> SearchQueries { get; set; } = [];

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
