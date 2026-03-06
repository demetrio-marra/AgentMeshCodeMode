using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface IAgentMemoryRetriever
    {
        Task<AgentMemoryRetrieverOutput> ExecuteAsync(AgentMemoryRetrieverInput input);
    }
}
