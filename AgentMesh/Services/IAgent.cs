using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IAgent
    {
        Task<AgentResultRecord> ExecuteAsync(IEnumerable<AgentInputParameterRecord> inputParameters, CancellationToken cancellationToken = default);
    }
}
