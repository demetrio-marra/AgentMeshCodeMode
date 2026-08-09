using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Application.Services.Agents
{
    public interface IAgentInputSerializer
    {
        IEnumerable<AgentMessage> SerializeInput(IEnumerable<IEWParameter> parameters, IEnumerable<AgentInputParameterConfiguration> parametersConfiguration);
    }
}
