namespace AgentMesh.Models.DocumentsCache
{
    public class GetAllCachedSearchesExecutorOutput
    {
        public IEnumerable<AgentMemoryCacheableQuery> AgentMemoryCachedQueries { get; set; } = [];
        public IEnumerable<KnowledgeBaseCacheableQuery> KnowledgeBaseCachedQueries { get; set; } = [];
    }
}
