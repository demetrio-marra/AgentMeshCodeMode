using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.DocumentsCache;

namespace AgentMesh.Models.AgentMemoryCacheSave
{
    public class AgentMemoryCacheSaveInput
    {
        public IEnumerable<AgentMemoryCacheableQuery> AgentMemoryCachedQueries { get; set; } = [];
        public AgentMemoryQueryResult AgentMemoryCachedQueryResult { get; set; } = new AgentMemoryQueryResult();
    }
}
