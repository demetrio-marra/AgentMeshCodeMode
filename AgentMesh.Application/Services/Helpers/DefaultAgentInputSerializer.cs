using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Utils;
using AgentMesh.Models;
using System.Text.Json;

namespace AgentMesh.Application.Services.Helpers
{
    public class DefaultAgentInputSerializer(IEnumerable<IEWParameterConfiguration> parameterConfigurations) : IAgentInputSerializer
    {
        public IEnumerable<AgentMessage> SerializeInput(IReadOnlyDictionary<Type, object?> parameters, IEnumerable<AgentInputParameterConfiguration> parameterTagsConfiguration)
        {
            var ret = new List<AgentMessage>();

            var pmc = parameterTagsConfiguration.ToDictionary(c => c.ParameterType, c => c.ParameterTags);

            var systemMessages = new List<string>();
            var userPayload = new Dictionary<string, string>();

            foreach (var parameter in parameters)
            {
                var config = pmc.GetValueOrDefault(parameter.Key);
                bool isSystemParameter = config != null && config.Contains(ParameterTags.AgentSystemParameterTag, StringComparer.InvariantCultureIgnoreCase);

                var parameterConfiguration = parameterConfigurations.FirstOrDefault(t => t.GetType() == parameter.Key);
                if (parameterConfiguration == null)
                {
                    throw new InvalidOperationException($"No parameter configuration found for parameter type {parameter.Key.FullName}");
                }

                if (isSystemParameter)
                {
                    systemMessages.Add($"{parameterConfiguration.Name}: {parameterConfiguration.ValueSerializer.Serialize(parameter.Value)}");
                }
                else
                {
                    userPayload.Add(parameterConfiguration.Name, parameterConfiguration.ValueSerializer.Serialize(parameter.Value));
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
