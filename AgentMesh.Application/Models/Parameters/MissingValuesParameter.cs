using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class MissingValuesParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<string>>
    {
        public override string Name => "Missing values";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
