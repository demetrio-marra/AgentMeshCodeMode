namespace AgentMesh.Models
{
    public class ContextAnalyzerAgentInput
    {
        public List<ContextMessage> ContextMessages { get; set; } = new List<ContextMessage>();
        public string UserLastRequest { get; set; } = string.Empty;
    }
}
