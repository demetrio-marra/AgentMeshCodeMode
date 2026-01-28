namespace AgentMesh.Services
{
    public interface IWorkflowProgressNotifier
    {
        Task NotifyWorkflowStart();
        Task NotifyWorkflowEnd();
        Task NotifyWorkflowStepStart(string stepName, Dictionary<string, string> inputParameters);
        Task NotifyWorkflowStepEnd(string stepName, Dictionary<string, string> outputParameters);
    }
}
