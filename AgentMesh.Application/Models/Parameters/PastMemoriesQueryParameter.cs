using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class PastMemoriesQueryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<AgentMemoryItem>>
    {
        public override string Name => "Past memories query";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
