using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IEWStep
    {
        string Name { get; }

        bool IsAgentic { get; }
        string? AgentName { get; }

        bool IsInputStep { get; }

        bool IsOutputStep { get; }

        IEnumerable<EWParameterFlags> ParametersConfiguration { get; }

        Task<EWResultRecord> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
