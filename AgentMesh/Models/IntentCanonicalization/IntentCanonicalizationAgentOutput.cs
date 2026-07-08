using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.IntentExtractor;

namespace AgentMesh.Models.IntentCanonicalization
{
    public class IntentCanonicalizationAgentOutput : IAgentOutput
    {
        public string DomainedIntent { get; set; } = string.Empty;
        public UserIntentCategoryValues CanonicalizedIntentCategory { get; set; }
        public IEnumerable<KnowledgeBaseQueryInputItem> CanonicalizedAPIQueries { get; set; } = [];

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
