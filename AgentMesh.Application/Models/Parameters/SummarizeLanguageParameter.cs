using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class SummarizeLanguageParameter : EWParameter<string>
    {
        public const string ParamName = "Summarize in language";
        public SummarizeLanguageParameter()
        {
            Name = ParamName;
        }
    }
}
