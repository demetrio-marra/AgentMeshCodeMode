using AgentMesh.Application.Models;
using AgentMesh.Models;

namespace AgentMesh.Application.Workflows
{
    public interface IWorkflow
    {
        Task<WorkflowResult> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory);
        string GetIngressAgentName();  
        string GetEgressAgentName();
    }

    public class WorkflowResult
    {
        public string Response { get; set; } = string.Empty;
        public List<AgentTokenUsageEntry> TokenUsageEntries { get; set; } = new();
    }
}
