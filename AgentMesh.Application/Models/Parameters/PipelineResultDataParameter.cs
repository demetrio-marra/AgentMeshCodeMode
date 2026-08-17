using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class PipelineResultDataParameter : EWParameter<string>
    {
        public const string ParamName = "Pipeline result data";
        public PipelineResultDataParameter()
        {
            Name = ParamName;
        }
    }
}
