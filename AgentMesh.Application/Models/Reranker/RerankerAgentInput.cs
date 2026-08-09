using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.RequestAnalysis;

namespace AgentMesh.Application.Models.Reranker
{
    public class RerankerAgentInput
    {
        public StructuredUserRequest StructuredUserRequest { get; set; } = new();
        public IEnumerable<KnowledgeBaseQueryResultItem> QueryResults { get; set; } = [];
    }
}
