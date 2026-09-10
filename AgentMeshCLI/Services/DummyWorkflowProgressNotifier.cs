using AgentMesh.Models;

namespace AgentMesh.Services
{
    internal sealed class DummyWorkflowProgressNotifier : IWorkflowProgressNotifier
    {
        public Task NotifyWorkflowStart() => Task.CompletedTask;

        public Task NotifyWorkflowEnd() => Task.CompletedTask;

        public Task NotifyWorkflowStepStarted(string stepName, IEnumerable<EWDisplayParameterRecord> inputParameters) => Task.CompletedTask;

        public Task NotifyWorkflowStepCompleted(string stepName, EWStepStatisticsRecord statistics) => Task.CompletedTask;
    }
}
