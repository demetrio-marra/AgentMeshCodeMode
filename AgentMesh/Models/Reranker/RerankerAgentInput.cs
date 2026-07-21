using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Models.Reranker
{
    public class RerankerAgentInput
    {
        public StructuredUserRequest StructuredUserRequest { get; set; } = new();
        public IEnumerable<KnowledgeBaseQueryResultItem> QueryResults { get; set; } = [];
    }
}
