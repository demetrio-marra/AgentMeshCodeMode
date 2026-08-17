using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class UserIntentParameter : EWParameter<string>
    {
        public const string ParamName = "User intent";
        public UserIntentParameter()
        {
            Name = ParamName;
        }
    }
}
