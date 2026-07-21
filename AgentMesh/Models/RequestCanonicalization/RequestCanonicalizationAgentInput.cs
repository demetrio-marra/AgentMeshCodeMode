using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Utils;

namespace AgentMesh.Models.RequestCanonicalization
{
    public class RequestCanonicalizationAgentInput
    {
        public StructuredUserRequest StructuredUserRequest { get; set; } = new();
        public IEnumerable<KnowledgeBaseQueryInputItem> DomainsKnowledgeBaseQuery { get; set; } = [];
        public string DomainsKnowledgeBaseDocumentsContent { get; set; } = string.Empty;
        public string LanguageOfKnowledgeBase { get; set; } = string.Empty;
        public string? QmdQueryTypesReference { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Structured user request", System.Text.Json.JsonSerializer.Serialize(StructuredUserRequest) },
                { "Domains knowledge base query", DomainsKnowledgeBaseQuery.Any() ? ListsFormatter.ToBulletList(DomainsKnowledgeBaseQuery.Select(query => query.ToString())) : "(No queries specified)" },
                { "Domains knowledge base documents content", $"Size: {DomainsKnowledgeBaseDocumentsContent.Length}" },
                { "Language of knowledge base", LanguageOfKnowledgeBase }
            };
        }
    }
}
