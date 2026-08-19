using AgentMesh.Models;
using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class IntentCategoryParameter : BaseEWParameterConfiguration<UserIntentCategory?>
    {
        public override string Name => "Intent category";
    }
}
