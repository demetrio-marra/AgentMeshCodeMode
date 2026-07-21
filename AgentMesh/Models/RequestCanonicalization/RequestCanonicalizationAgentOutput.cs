using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Models.RequestCanonicalization
{
    public class RequestCanonicalizationAgentOutput : IAgentOutput
    {
        public AgentMesh.Models.RequestAnalysis.StructuredUserRequest CanonicalizedStructuredUserRequest { get; set; } = new();
        public UserIntentCategory CanonicalizedIntentCategory { get; set; }
        public IEnumerable<KnowledgeBaseQueryInputItem> CanonicalizedDomainsKnowledgeBaseQuery { get; set; } = [];

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
