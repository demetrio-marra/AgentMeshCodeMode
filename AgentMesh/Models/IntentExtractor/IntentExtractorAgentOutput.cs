namespace AgentMesh.Models.IntentExtractor
{
    public class IntentExtractorAgentOutput : IAgentOutput
    {
        public string UserIntent { get; set; } = string.Empty;
        public string? LanguageOfTheUser { get; set; }
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public IEnumerable<string> UserRequestDomains { get; set; } = [];

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
