using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class RequestRejectedReasonParameter : EWParameter<string>
    {
        public const string ParamName = "Request rejected reason";
        public RequestRejectedReasonParameter()
        {
            Name = ParamName;
        }
    }
}
