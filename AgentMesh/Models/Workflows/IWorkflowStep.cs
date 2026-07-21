namespace AgentMesh.Models.Workflows
{
    public interface IWorkflowStep<T>
    {
        Task<WorkflowStepUsageEntry> ExecuteAsync(T stateObject, CancellationToken cancellationToken = default);
    }
}
