namespace AgentMesh.Models.DocumentsCache
{
    public class DocumentsCacheExecutorInput
    {
        public IEnumerable<AgentMemoryCacheableQuery>? AgentMemoryCachedQueries { get; set; }
        public IEnumerable<KnowledgeBaseCacheableQuery>? KnowledgeBaseCachedQueries { get; set; }
    }
}
