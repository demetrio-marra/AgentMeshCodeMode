using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class SandboxExecutionIdParameter : BaseEWParameterConfiguration<string>
    {
        public override string Name => "Code execution id";
    }
}
