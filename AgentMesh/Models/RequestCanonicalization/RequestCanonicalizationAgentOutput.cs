using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Utils;

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

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Canonicalized structured user request", System.Text.Json.JsonSerializer.Serialize(CanonicalizedStructuredUserRequest) },
                { "Canonicalized intent category", CanonicalizedIntentCategory.ToString() },
                { "Canonicalized domains knowledge base query", CanonicalizedDomainsKnowledgeBaseQuery.Any() ? ListsFormatter.ToBulletList(CanonicalizedDomainsKnowledgeBaseQuery.Select(query => query.ToString())) : "(No queries specified)" }
            };
        }
    }
}
