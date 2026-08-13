using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Utils;
using AgentMesh.Models;
using System.Text.Json;

namespace AgentMesh.Application.Services.Agents
{
    public class DefaultAgentInputSerializer : IAgentInputSerializer
    {
        public IEnumerable<AgentMessage> SerializeInput(IEnumerable<IEWParameter> parameters, IEnumerable<AgentInputParameterConfiguration> parametersConfiguration)
        {
            var ret = new List<AgentMessage>();

            var pmc = parametersConfiguration.ToDictionary(c => c.ParameterName, c => c.ParameterTags, StringComparer.InvariantCultureIgnoreCase);

            var systemMessages = new List<string>();
            var userPayload = new Dictionary<string, string>();

            foreach (var parameter in parameters)
            {
                var config = pmc.GetValueOrDefault(parameter.Name);
                bool isSystemParameter = config != null && config.Contains(ParameterTags.AgentSystemParameterTag, StringComparer.InvariantCultureIgnoreCase);

                if (isSystemParameter)
                {
                    systemMessages.Add($"{parameter.Name}: {parameter.Serialize()}");
                }
                else
                {
                    userPayload.Add(parameter.Name, parameter.Serialize());
                }
            }

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = string.Join(Environment.NewLine + Environment.NewLine, systemMessages) },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

            return inputMessages;
        }
    }
}
