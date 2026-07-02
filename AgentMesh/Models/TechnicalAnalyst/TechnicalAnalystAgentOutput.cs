namespace AgentMesh.Models.TechnicalAnalyst
{
    public class TechnicalAnalystAgentOutput : IAgentOutput
    {
        public string? BusinessRequirements { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
