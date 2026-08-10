using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IEWAgenticStep : IEWStep
    {
        string? AgentName { get; }

        bool IsInputTokensCountSource { get; }

        bool IsOutputTokensCountSource { get; }

        Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
