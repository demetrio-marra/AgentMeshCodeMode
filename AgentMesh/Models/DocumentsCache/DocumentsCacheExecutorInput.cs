namespace AgentMesh.Models.DocumentsCache
{
    public class DocumentsCacheExecutorInput
    {
        public AgentMemoryCachedQuery? AgentMemoryCachedQuery { get; set; } = null;
        public KnowledgeBaseCachedQuery? KnowledgeBaseCachedQuery { get; set; } = null;
    }
}
