namespace AgentMesh.Application.Workflows
{
    public interface IWorkflow
    {
        Task<WorkflowResult> ExecuteAsync(string userInput);
    }

    public class WorkflowResult
    {
        public string Response { get; set; } = string.Empty;
        public Dictionary<string, int> InputTokenUsage { get; set; } = new();
        public Dictionary<string, int> OutputTokenUsage { get; set; } = new();
    }
}
