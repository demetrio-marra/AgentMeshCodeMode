using AgentMesh.Application.Models;

namespace AgentMesh.Application.Contracts
{
    public interface IAgentMemoryRetriever
    {
        Task<AgentMemoryRetrieverOutput> ExecuteAsync(AgentMemoryRetrieverInput input);
    }
}
