namespace AgentMesh.Models.IntentExtractor
{
    public class IntentExtractorAgentOutput : IAgentOutput
    {
        public string UserIntent { get; set; } = string.Empty;
        public IEnumerable<string> Entities { get; set; } = [];
        public IEnumerable<string> Domains { get; set; } = [];
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public string LanguageOfTheUser { get; set; } = string.Empty;

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
