using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.Reranker
{
    public class RerankerAgentOutput : IAgentOutput
    {
        public IEnumerable<KnowledgeBaseQueryResultItem> QueryResults { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
