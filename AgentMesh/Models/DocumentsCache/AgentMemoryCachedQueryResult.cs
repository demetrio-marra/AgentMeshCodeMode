using AgentMesh.Models.AgentMemory;

namespace AgentMesh.Models.DocumentsCache
{
    public class AgentMemoryCachedQueryResult
    {
        public IEnumerable<AgentMemoryItem> Memories { get; set; } = [];
    }
}
