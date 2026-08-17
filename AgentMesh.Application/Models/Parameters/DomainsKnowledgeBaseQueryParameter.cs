using AgentMesh.Models;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class DomainsKnowledgeBaseQueryParameter : EWParameter<IEnumerable<KnowledgeBaseQueryInputItem>>
    {
        public const string ParamName = "Domain knowledge base queries";
        public DomainsKnowledgeBaseQueryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }
}
