using AgentMesh.Models.IntentExtractor;

namespace AgentMesh.Models.Workflows
{
    public interface IWorkflow
    {
        Task<WorkflowResult> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory);
        string GetIngressAgentName();  
        string GetEgressAgentName();
    }  
}
