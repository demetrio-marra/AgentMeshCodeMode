using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class AnalystDocumentationMissingEntitiesParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<string>>
    {
        public override string Name => "Analyst documentation missing entities";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
