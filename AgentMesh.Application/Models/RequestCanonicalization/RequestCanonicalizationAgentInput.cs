using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.RequestAnalysis;

namespace AgentMesh.Models.RequestCanonicalization
{
    public class RequestCanonicalizationAgentInput
    {
        public StructuredUserRequest StructuredUserRequest { get; set; } = new();
        public IEnumerable<KnowledgeBaseQueryInputItem> DomainsKnowledgeBaseQuery { get; set; } = [];
        public string DomainsKnowledgeBaseDocumentsContent { get; set; } = string.Empty;
        public string LanguageOfKnowledgeBase { get; set; } = string.Empty;
        public string? DocumentationQueriesGenerationReference { get; set; }
    }
}
