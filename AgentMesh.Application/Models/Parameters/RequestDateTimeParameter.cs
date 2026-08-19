using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class RequestDateTimeParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer) : BaseEWParameterConfiguration<DateTime>
    {
        public override string Name => "Current datetime";

        public override IEWParameterSerializer DisplayValueSerializer => displayValueSerializer;
    }
}
