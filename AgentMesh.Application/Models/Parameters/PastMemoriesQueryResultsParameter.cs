using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class PastMemoriesQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<AgentMemoryQueryResultItem>>
    {
        public override string Name => "Past memories query results";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
