using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class SummarizedContentDatetimeParameter : EWParameter<DateTime>
    {
        public const string ParamName = "Summarized content datetime";
        public SummarizedContentDatetimeParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }
}
