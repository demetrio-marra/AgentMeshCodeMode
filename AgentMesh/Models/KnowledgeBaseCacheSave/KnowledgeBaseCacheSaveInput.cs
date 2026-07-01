using AgentMesh.Models.DocumentsCache;

namespace AgentMesh.Models.KnowledgeBaseCacheSave
{
    public class KnowledgeBaseCacheSaveInput
    {
        public KnowledgeBaseCachedQuery? KnowledgeBaseCachedQuery { get; set; }
        public KnowledgeBaseCachedQueryResult? KnowledgeBaseCachedQueryResult { get; set; }
    }
}
