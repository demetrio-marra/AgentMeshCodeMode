using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.IntentExtractor;

namespace AgentMesh.Models.IntentCanonicalization
{
    public class IntentCanonicalizationAgentInput
    {
        public string Intent { get; set; } = string.Empty;
        public UserIntentCategoryValues UserIntentCategory { get; set; }
        public Dictionary<string, IEnumerable<string>> EntitiesByDomain { get; set; } = new();
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public string DomainDocumentationContents { get; set; } = string.Empty;
        public IEnumerable<KnowledgeBaseQueryInputItem> NonCanonicalizedQueries { get; set; } = [];
        public string LanguageOfKnowledgeBase { get; set; } = string.Empty;
    }
}
