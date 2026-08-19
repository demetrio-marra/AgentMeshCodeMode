using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class KnowledgeBaseAPIDocumentsContentParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<KnowledgeBaseDocumentContent>>
    {
        public override string Name => "API knowledge base documents";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
