using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class LanguageOfTheUserParameter : BaseEWParameterConfiguration<string>
    {
        public override string Name => "Language to respond to the user";
    }
}
