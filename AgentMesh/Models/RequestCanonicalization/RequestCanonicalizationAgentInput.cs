using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Models.RequestCanonicalization
{
    public class RequestCanonicalizationAgentInput
    {
        public StructuredUserRequest StructuredUserRequest { get; set; } = new();
        public IEnumerable<KnowledgeBaseQueryInputItem> DomainsKnowledgeBaseQuery { get; set; } = [];
        public string DomainsKnowledgeBaseDocumentsContent { get; set; } = string.Empty;
        public string LanguageOfKnowledgeBase { get; set; } = string.Empty;
        public string? QmdQueryTypesReference { get; set; }
    }
}
