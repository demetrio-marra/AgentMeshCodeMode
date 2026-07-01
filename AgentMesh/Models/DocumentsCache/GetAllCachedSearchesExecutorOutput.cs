namespace AgentMesh.Models.DocumentsCache
{
    public class GetAllCachedSearchesExecutorOutput
    {
        public IEnumerable<AgentMemoryCachedQuery> AgentMemoryCachedQueries { get; set; } = [];
        public IEnumerable<KnowledgeBaseCachedQuery> KnowledgeBaseCachedQueries { get; set; } = [];
    }
}
