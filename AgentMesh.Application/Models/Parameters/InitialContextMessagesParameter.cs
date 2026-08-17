using AgentMesh.Models;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class InitialContextMessagesParameter : EWParameter<IEnumerable<ContextMessage>>
    {
        public const string ParamName = "Initial context messages";
        public InitialContextMessagesParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            IsConversationHistoryParameter = true;
            DisplayValueSerializer = displayValueSerializer;
        }
    }
}

