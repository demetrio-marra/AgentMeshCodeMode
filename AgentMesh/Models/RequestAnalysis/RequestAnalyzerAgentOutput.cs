namespace AgentMesh.Models.RequestAnalysis
{
    public class RequestAnalyzerAgentOutput : StructuredUserRequest, IAgentOutput
    {
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
