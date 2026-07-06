namespace AgentMesh.Models.IntentExtractor
{
    public class IntentExtractorAgentOutput : IAgentOutput
    {
        public string UserIntent { get; set; } = string.Empty;
        public UserIntentCategoryValues UserIntentCategory { get; set; }
        public Dictionary<string, IEnumerable<string>> EntitiesByDomain { get; set; } = new();
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public IEnumerable<string> UserPreferences { get; set; } = [];
        public IEnumerable<string> MissingMemories { get; set; } = [];
        public string LanguageOfTheUser { get; set; } = string.Empty;

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
