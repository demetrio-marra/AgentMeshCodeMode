using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface IAgentMemorySaver
    {
        Task ExecuteAsync(AgentMemorySaverInput input);
    }
}
