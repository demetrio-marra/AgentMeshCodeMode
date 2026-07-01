using AgentMesh.Models.DocumentsCache;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.KnowledgeBaseCacheSave
{
    public class KnowledgeBaseCacheSaveInput
    {
        public IEnumerable<KnowledgeBaseCacheableQuery>? KnowledgeBaseCachedQueries { get; set; }
        public KnowledgeBaseQueryResult? KnowledgeBaseCachedQueryResult { get; set; }
    }
}
