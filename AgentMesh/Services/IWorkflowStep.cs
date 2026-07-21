using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IWorkflowStep<T>
    {
        Task<WorkflowStepUsageEntry> ExecuteAsync(T stateObject, CancellationToken cancellationToken = default);
    }
}
