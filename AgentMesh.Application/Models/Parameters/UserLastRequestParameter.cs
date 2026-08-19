using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class UserLastRequestParameter : BaseEWParameterConfiguration<string>
    {
        public override string Name => "User last request";
    }
}
