using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class SandboxExecutionIdParameter : EWParameter<string>
    {
        public const string ParamName = "Code execution id";
        public SandboxExecutionIdParameter()
        {
            Name = ParamName;
        }
    }
}
