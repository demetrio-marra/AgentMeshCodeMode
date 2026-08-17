using AgentMesh.Models;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class PastMemoriesQueryParameter : EWParameter<IEnumerable<AgentMemoryItem>>
    {
        public const string ParamName = "Past memories query";
        public PastMemoriesQueryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }
}
