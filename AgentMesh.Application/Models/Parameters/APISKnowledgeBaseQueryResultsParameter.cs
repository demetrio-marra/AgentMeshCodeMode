using AgentMesh.Models;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class APISKnowledgeBaseQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public override string Name => "API knowledge base query results";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
