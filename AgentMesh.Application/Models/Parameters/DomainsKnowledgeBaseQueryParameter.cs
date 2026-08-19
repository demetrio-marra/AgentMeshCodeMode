using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class DomainsKnowledgeBaseQueryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<KnowledgeBaseQueryInputItem>>
    {
        public override string Name => "Domain knowledge base queries";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
