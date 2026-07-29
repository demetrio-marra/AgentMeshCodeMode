using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IEasyWorkflowStep
    {
        string Name { get; }

        bool IsAgentic { get; }

        bool IsInputStep { get; }

        bool IsOutputStep { get; }

        IEnumerable<string> RequiredParameterNames { get; }

        Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default);
    }
}
