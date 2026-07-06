using AgentMesh.Models.AgentMemory;

namespace AgentMesh.Services
{
    public interface IAgentMemoryRetrieverExecutor
    {
        Task<AgentMemoryRetrieverOutput> ExecuteAsync(AgentMemoryRetrieverInput input);
    }
}
