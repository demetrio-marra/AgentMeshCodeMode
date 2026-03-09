using AgentMesh.Application.Models;

namespace AgentMesh.Application.Contracts
{
    public interface IAgentMemorySaver
    {
        Task ExecuteAsync(AgentMemorySaverInput input);
    }
}
