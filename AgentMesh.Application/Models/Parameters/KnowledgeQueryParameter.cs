using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class KnowledgeQueryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<KnowledgeQuery>
    {
        public override string Name => "Knowledge query";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
