using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class DomainsKnowledgeBaseDocumentsContentParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<KnowledgeBaseDocumentContent>>
    {
        public override string Name => "Domain knowledge base documents";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
