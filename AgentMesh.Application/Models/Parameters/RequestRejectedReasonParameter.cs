using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class RequestRejectedReasonParameter : BaseEWParameterConfiguration<string>
    {
        public override string Name => "Request rejected reason";
    }
}
