using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class KnowledgeContentForCoderParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<KnowledgeContentItem>>
    {
        public override string Name => "Knowledge content for Coder Agent";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
