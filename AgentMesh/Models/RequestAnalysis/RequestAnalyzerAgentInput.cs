namespace AgentMesh.Models.RequestAnalysis
{
    public class RequestAnalyzerAgentInput
    {
        public List<ContextMessage> ContextMessages { get; set; } = [];
        public string UserLastRequest { get; set; } = string.Empty;
    }
}
