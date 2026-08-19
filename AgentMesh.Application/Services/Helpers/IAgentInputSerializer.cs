using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.ChatMessages;

namespace AgentMesh.Application.Services.Helpers
{
    public interface IAgentInputSerializer
    {
        IEnumerable<AgentMessage> SerializeInput(IReadOnlyDictionary<Type, object?> parameters, 
            IEnumerable<AgentInputParameterConfiguration> parameterTagsConfiguration);
    }
}
