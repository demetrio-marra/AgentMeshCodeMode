using AgentMesh.Application.Models;

namespace AgentMesh.Application.Services
{
    public interface IAgentMemoryRetriever
    {
        Task<AgentMemoryRetrieverOutput> ExecuteAsync(AgentMemoryRetrieverInput input);
    }
}
