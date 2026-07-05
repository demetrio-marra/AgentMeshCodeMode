using AgentMesh.Models.KnowledgeBase;
using static AgentMesh.Models.IntentExtractor.IntentExtractorAgentOutput;

namespace AgentMesh.Models.RequirementsCollector
{
    public class RequirementsCollectorAgentInput
    {
        public string UserIntent { get; set; } = string.Empty;
        public UserIntentCategoryValues UserIntentCategory { get; set; }
        public Dictionary<string, IEnumerable<string>> EntitiesByDomain { get; set; } = new();
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public IEnumerable<string> UserPreferences { get; set; } = [];
        public IEnumerable<string> MissingMemories { get; set; } = [];
        public IEnumerable<KnowledgeBaseQueryResultItem> FastKnowledgeBaseQueryResults { get; set; } = [];
    }
}
