using AgentMesh.Models;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class APISKnowledgeBaseQueryResultsParameter : EWParameter<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public const string ParamName = "API knowledge base query results";
        public APISKnowledgeBaseQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }
}
