using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class SummarizeLanguageParameter : BaseEWParameterConfiguration<string>
    {
        public override string Name => "Summarize in language";
    }
}
