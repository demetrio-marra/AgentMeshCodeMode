using AgentMesh.Models;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IWorkflow
    {
        Task<WorkflowResult> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory);
        string GetIngressExecutorName();
        string GetEgressExecutorName();
    }
}
