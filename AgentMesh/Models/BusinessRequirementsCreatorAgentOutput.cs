namespace AgentMesh.Models
{
    public class BusinessRequirementsCreatorAgentOutput : IAgentOutput
    {
        public string? BusinessRequirements { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
