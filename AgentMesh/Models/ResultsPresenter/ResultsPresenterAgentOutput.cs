namespace AgentMesh.Models.ResultsPresenter
{
    public class ResultsPresenterAgentOutput : IAgentOutput
    {
        public string Content { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
