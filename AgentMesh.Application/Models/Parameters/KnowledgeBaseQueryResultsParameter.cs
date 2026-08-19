using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class KnowledgeBaseQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public override string Name => "Knowledge base query results";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
