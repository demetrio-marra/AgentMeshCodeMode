using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class RequestDateTimeParameter : EWParameter<DateTime>
    {
        public const string ParamName = "Current datetime";
        public RequestDateTimeParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            ParameterValue = DateTime.UtcNow;
            DisplayValueSerializer = displayValueSerializer;
        }
    }
}
