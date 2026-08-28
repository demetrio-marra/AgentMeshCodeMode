using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class KnowledgeQueryResultParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<KnowledgeQueryResult>
    {
        public override string Name => "Knowledge query result";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
