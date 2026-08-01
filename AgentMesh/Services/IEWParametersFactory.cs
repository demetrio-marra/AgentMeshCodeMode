using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IEWParametersFactory
    {
        IEnumerable<IEWParameter> CreateParameters();
    }
}
