using AgentMesh.Models;
using AgentMesh.Application.Models.CodeSandbox;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class CodeExecutionResultTypeParameter : EWParameter<SandboxResultType>
    {
        public const string ParamName = "Code execution result type";
        public CodeExecutionResultTypeParameter()
        {
            Name = ParamName;
        }
    }
}
