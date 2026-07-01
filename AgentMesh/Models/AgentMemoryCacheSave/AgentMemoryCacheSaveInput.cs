using AgentMesh.Models.DocumentsCache;

namespace AgentMesh.Models.AgentMemoryCacheSave
{
    public class AgentMemoryCacheSaveInput
    {
        public AgentMemoryCachedQuery? AgentMemoryCachedQuery { get; set; }
        public AgentMemoryCachedQueryResult? AgentMemoryCachedQueryResult { get; set; }
    }
}
