using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class RequestRejectedFlagParameter : BaseEWParameterConfiguration<bool>
    {
        public override string Name => "Request rejected flag";
    }
}
