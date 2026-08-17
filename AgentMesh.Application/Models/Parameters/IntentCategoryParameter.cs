using AgentMesh.Models;
using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class IntentCategoryParameter : EWParameter<UserIntentCategory?>
    {
        public const string ParamName = "Intent category";
        public IntentCategoryParameter()
        {
            Name = ParamName;
        }
    }
}
