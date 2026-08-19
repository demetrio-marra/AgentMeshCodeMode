using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class CodeExecutionResultTypeParameter : BaseEWParameterConfiguration<SandboxResultType>
    {
        public override string Name => "Code execution result type";
    }
}
