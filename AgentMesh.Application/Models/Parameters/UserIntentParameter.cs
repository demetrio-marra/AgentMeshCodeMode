using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class UserIntentParameter : BaseEWParameterConfiguration<string>
    {
        public override string Name => "User intent";
    }
}
