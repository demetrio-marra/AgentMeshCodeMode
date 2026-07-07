namespace AgentMesh.Models.IntentCanonicalization
{
    public class IntentCanonicalizationAgentOutput : IAgentOutput
    {
        public string DomainedIntent { get; set; } = string.Empty;
        public AgentMesh.Models.IntentExtractor.UserIntentCategoryValues CanonicalizedIntentCategory { get; set; }

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
