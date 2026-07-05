namespace AgentMesh.Models.DomainExpert
{
    public class DomainExpertAgentOutput : IAgentOutput
    {
        public string BusinessRequirements { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
