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
        public string? DocumentationQueriesGenerationReference { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            var dictionary = StructuredUserRequest.ToDictionary();
            dictionary.Add("Domains knowledge base query", DomainsKnowledgeBaseQuery.Any() ? ListsFormatter.ToBulletList(DomainsKnowledgeBaseQuery.Select(query => query.ToString())) : "(No queries specified)");
            dictionary.Add("Domains knowledge base documents content", $"Size: {DomainsKnowledgeBaseDocumentsContent.Length}");
            dictionary.Add("Language of knowledge base", LanguageOfKnowledgeBase);

            return dictionary;
        }
    }
}
