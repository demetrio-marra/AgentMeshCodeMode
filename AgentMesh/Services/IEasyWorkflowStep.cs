using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IEasyWorkflowStep
    {
        string Name { get; }

        bool IsAgentic { get; }
        string? AgentName { get; }

        bool IsInputStep { get; }

        bool IsOutputStep { get; }

        IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames { get; }

        Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default);
    }
}
