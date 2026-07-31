using AgentMesh.Models.Workflows;

namespace AgentMesh.Application.Services
{
    public interface IAgent
    {
        Task<AgentResultRecord> ExecuteAsync(IEnumerable<AgentInputParameterRecord> inputParameters, CancellationToken cancellationToken = default);
    }
}
