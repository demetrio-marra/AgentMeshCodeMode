using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface IEWAgenticStep : IEWStep
    {
        string? AgentName { get; }

        bool IsInputTokensCountSource { get; }

        bool IsOutputTokensCountSource { get; }

        Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
