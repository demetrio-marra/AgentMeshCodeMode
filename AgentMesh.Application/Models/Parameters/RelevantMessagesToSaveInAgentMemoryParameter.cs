using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class RelevantMessagesToSaveInAgentMemoryParameter : EWParameter<IEnumerable<ContextMessage>>
    {
        public const string ParamName = "Relevant messages to save in agent memory";
        public RelevantMessagesToSaveInAgentMemoryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }
}
