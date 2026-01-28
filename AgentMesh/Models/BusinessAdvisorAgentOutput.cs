namespace AgentMesh.Models
{
    public class BusinessAdvisorAgentOutput : IAgentOutput
    {
        public bool EngageCoderAgent { get; set; }
        public string? AnswerToUserText { get; set; }
        public string? BusinessRequirements { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
