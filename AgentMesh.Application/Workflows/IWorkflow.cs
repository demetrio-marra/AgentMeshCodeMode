using AgentMesh.Application.Models;

namespace AgentMesh.Application.Workflows
{
    public interface IWorkflow
    {
        Task<WorkflowResult> ExecuteAsync(string userInput);
    }

    public class WorkflowResult
    {
        public string Response { get; set; } = string.Empty;
        public List<AgentTokenUsageEntry> TokenUsageEntries { get; set; } = new();
    }
}
