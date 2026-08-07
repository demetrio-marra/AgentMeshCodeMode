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

        IEnumerable<string> InputParameters { get; }

        Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default);
    }
}
