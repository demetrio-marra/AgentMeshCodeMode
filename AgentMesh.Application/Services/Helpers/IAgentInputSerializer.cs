using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Models;

namespace AgentMesh.Application.Services.Helpers
{
    public interface IAgentInputSerializer
    {
        IEnumerable<AgentMessage> SerializeInput(IEnumerable<IEWParameter> parameters, IEnumerable<AgentInputParameterConfiguration> parametersConfiguration);
    }
}
