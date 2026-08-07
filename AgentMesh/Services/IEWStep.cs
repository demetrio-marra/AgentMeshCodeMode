using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IEWStep
    {
        string Name { get; }

        bool IsAgentic { get; }

        string? AgentName { get; }

        bool IsPipelineFirst { get; }

        bool IsPipelineLast { get; }

        Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
