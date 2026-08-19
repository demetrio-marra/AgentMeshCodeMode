using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class ExecutionErrorParameter : BaseEWParameterConfiguration<bool>
    {
        public override string Name => "Code execution error occurred flag";
    }
}
