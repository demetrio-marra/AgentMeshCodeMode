using AgentMesh.Models;
using AgentMesh.Utils;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Models.RequestCanonicalization
{
    public class RequestCanonicalizationAgentOutput : IAgentOutput
    {
        public StructuredUserRequest CanonicalizedStructuredUserRequest { get; set; } = new();
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
