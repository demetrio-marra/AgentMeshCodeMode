using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface IWorkflowProgressNotifier
    {
        Task NotifyWorkflowStart();
        Task NotifyWorkflowEnd();
        Task NotifyWorkflowStepStarted(string stepName, IEnumerable<EWDisplayParameterRecord> inputParameters);
        Task NotifyWorkflowStepCompleted(string stepName, EWStepStatisticsRecord statistics);
    }
}
