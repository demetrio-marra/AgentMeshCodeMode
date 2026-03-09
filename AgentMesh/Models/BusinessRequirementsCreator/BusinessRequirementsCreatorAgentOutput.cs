namespace AgentMesh.Models.BusinessRequirementsCreator
{
    public class BusinessRequirementsCreatorAgentOutput : IAgentOutput
    {
        public string? BusinessRequirements { get; set; }
        public IEnumerable<string> MentionedApis { get; set; } = Enumerable.Empty<string>();
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
