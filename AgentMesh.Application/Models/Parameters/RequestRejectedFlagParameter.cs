using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class RequestRejectedFlagParameter : EWParameter<bool>
    {
        public const string ParamName = "Request rejected flag";
        public RequestRejectedFlagParameter()
        {
            Name = ParamName;
        }
    }
}
