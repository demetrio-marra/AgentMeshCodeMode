using AgentMesh.Application.Models;

namespace AgentMesh.Application.Services
{
    public interface IAgentMemorySaver
    {
        Task ExecuteAsync(AgentMemorySaverInput input);
    }
}
