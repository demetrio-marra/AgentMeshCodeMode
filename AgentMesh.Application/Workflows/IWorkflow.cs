using AgentMesh.Application.Models;
using AgentMesh.Models;

namespace AgentMesh.Application.Workflows
{
    public interface IWorkflow
    {
        Task<WorkflowResult> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory);
    }

    public class WorkflowResult
    {
        public string Response { get; set; } = string.Empty;
        public List<AgentTokenUsageEntry> TokenUsageEntries { get; set; } = new();
        public string IngressAgentName { get; set; } = string.Empty;
        public string EgressAgentName { get; set; } = string.Empty;
    }
}
