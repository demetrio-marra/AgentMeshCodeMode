using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class GeneratedCodeParameter : EWParameter<string>
    {
        public const string ParamName = "Generated code";
        public GeneratedCodeParameter()
        {
            Name = ParamName;
        }
    }
}
