using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class ExecutionErrorParameter : EWParameter<bool>
    {
        public const string ParamName = "Code execution error occurred flag";
        public ExecutionErrorParameter()
        {
            Name = ParamName;
        }
    }
}
