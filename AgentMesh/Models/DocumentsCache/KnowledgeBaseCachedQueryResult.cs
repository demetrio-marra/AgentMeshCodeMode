using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.DocumentsCache
{
    public class KnowledgeBaseCachedQueryResult
    {
        public IEnumerable<KnowledgeBaseQueryResultItem> Results { get; set; } = [];
    }
}
