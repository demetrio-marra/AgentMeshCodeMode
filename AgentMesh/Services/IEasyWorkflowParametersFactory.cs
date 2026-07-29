using AgentMesh.Models.Parameters;

namespace AgentMesh.Services
{
    public interface IEasyWorkflowParametersFactory
    {
        IEnumerable<Parameter> CreateParameters();
    }
}
