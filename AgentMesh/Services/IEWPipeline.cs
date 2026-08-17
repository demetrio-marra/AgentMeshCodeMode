using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface IEWPipeline
    {
        Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}