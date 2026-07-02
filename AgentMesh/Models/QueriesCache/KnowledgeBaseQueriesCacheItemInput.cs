using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.QueriesCache
{
    public class KnowledgeBaseQueriesCacheItemInput
    {
        public string Query { get; set; } = string.Empty;
        public KnowledgeBaseQuerySearchType QueryType { get; set; }
    }
}
