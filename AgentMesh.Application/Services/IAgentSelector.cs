using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public interface IAgentSelector
    {
        IAgent GetAgent(string agentName);
    }
}
