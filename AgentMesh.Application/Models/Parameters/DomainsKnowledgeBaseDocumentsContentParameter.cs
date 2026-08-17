using AgentMesh.Models;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class DomainsKnowledgeBaseDocumentsContentParameter : EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>
    {
        public const string ParamName = "Domain knowledge base documents";
        public DomainsKnowledgeBaseDocumentsContentParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }
}
