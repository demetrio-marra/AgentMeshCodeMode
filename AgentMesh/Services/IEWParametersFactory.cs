using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IEWParametersFactory
    {
        IEnumerable<EWParameter> CreateParameters();
    }
}
