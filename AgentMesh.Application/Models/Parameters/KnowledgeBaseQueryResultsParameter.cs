using AgentMesh.Models;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class KnowledgeBaseQueryResultsParameter : EWParameter<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public const string ParamName = "Knowledge base query results";
        public KnowledgeBaseQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }
}
