using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class SummarizedContentParameter : EWParameter<string>
    {
        public const string ParamName = "Summarized content";
        public SummarizedContentParameter()
        {
            Name = ParamName;
        }
    }
}
