using static AgentMesh.Models.IntentExtractor.IntentExtractorAgentOutput;

namespace AgentMesh.Application.Models
{
    /// <summary>
    /// The request from the user, structured and classified into intent, categories, entities, and other relevant information for processing by the system.
    /// </summary>
    public class StructuredUserRequest
    {
        public string Intent { get; set; } = string.Empty;
        public UserIntentCategoryValues IntentCategory { get; set; }
        public Dictionary<string, IEnumerable<string>> EntitiesByDomain { get; set; } = new();
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public IEnumerable<string> UserPreferences { get; set; } = [];
        public IEnumerable<string> MissingMemories { get; set; } = [];
        public string LanguageOfTheUser { get; set; } = string.Empty;
    }
}
