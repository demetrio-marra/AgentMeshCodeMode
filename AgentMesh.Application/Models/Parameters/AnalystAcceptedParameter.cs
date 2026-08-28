using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class AnalystAcceptedParameter : BaseEWParameterConfiguration<bool>
    {
        public override string Name => "Analyst accepted";
    }
}
