namespace AgentMesh.Models.IntentCanonicalization
{
    public class IntentCanonicalizationAgentOutput : IAgentOutput
    {
        public string DomainedIntent { get; set; } = string.Empty;

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
