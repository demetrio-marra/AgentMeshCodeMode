namespace AgentMesh.Models.TechnicalAnalyst
{
    public class TechnicalAnalystAgentOutput : IAgentOutput
    {
        public string TechnicalSpecification { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
