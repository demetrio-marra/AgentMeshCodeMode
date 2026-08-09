using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Application.Services.Agents
{
    public class DefaultAgentInputSerializer : IAgentInputSerializer
    {
        public IEnumerable<AgentMessage> SerializeInput(IEnumerable<IEWParameter> parameters, IEnumerable<AgentInputParameterConfiguration> parametersConfiguration)
        {
            var ret = new List<AgentMessage>();

            var pmc = parametersConfiguration.ToDictionary(c => c.ParameterName, c => c.ParameterTags, StringComparer.InvariantCultureIgnoreCase);

            foreach (var parameter in parameters)
            {
                var config = pmc.GetValueOrDefault(parameter.Name);
                bool isSystemParameter = config != null && config.Contains(ApplicationConstants.AgentSystemParameterTag, StringComparer.InvariantCultureIgnoreCase);
              
                var message = new AgentMessage
                {
                    Role = isSystemParameter ? AgentMessageRole.System : AgentMessageRole.User,
                    Content = parameter.GetDisplayValue()
                };
                ret.Add(message);
            }

            return ret;
        }
    }
}
