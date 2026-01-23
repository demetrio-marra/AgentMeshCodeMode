namespace AgentMesh.Models
{
    public class ResultsPresenterAgentOutput : IAgentOutput
    {
        public string Answer { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
