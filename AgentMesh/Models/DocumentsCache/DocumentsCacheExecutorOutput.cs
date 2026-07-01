using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.DocumentsCache
{
    public class DocumentsCacheExecutorOutput
    {
        public AgentMemoryQueryResult? AgentMemoryCachedQueryResult { get; set; }
        public KnowledgeBaseQueryResult? KnowledgeBaseCachedQueryResult { get; set; }
    }
}
