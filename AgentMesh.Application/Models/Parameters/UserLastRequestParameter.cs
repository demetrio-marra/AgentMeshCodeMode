using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class UserLastRequestParameter : EWParameter<string>
    {
        public const string ParamName = "User last request";
        public UserLastRequestParameter()
        {
            Name = ParamName;
            IsUserCurrentRequestParameter = true;
        }
    }
}
