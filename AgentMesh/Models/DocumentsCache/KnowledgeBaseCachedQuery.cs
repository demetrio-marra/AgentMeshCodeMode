namespace AgentMesh.Models.DocumentsCache
{
    public class KnowledgeBaseCachedQuery
    {
        public IEnumerable<AgentMemoryCachedQuery> AgentMemoryCachedQueries { get; set; } = [];
        public IEnumerable<KnowledgeBaseCachedQuery> KnowledgeBaseCachedQueries { get; set; } = [];

    }
}
