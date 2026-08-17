using AgentMesh.Models;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class PastMemoriesQueryResultsParameter : EWParameter<IEnumerable<AgentMemoryQueryResultItem>>
    {
        public const string ParamName = "Past memories query results";
        public PastMemoriesQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }
}
