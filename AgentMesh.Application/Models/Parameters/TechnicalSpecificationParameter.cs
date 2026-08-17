using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class TechnicalSpecificationParameter : EWParameter<string>
    {
        public const string ParamName = "Technical specification";
        public TechnicalSpecificationParameter()
        {
            Name = ParamName;
        }
    }
}
