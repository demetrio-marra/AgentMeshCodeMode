using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class BusinessRequirementsParameter : EWParameter<string>
    {
        public const string ParamName = "Business requirements";
        public BusinessRequirementsParameter()
        {
            Name = ParamName;
        }
    }
}
