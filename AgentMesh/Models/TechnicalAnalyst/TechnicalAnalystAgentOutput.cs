namespace AgentMesh.Models.TechnicalAnalyst
{
    public class TechnicalAnalystAgentOutput : IAgentOutput
    {
        public string TechnicalSpecification { get; set; } = string.Empty;
        public required bool RequestRejected { get; set; }
        public string? ReasonOfRejection { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
