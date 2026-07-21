using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Infrastructure.QDrant
{
    internal class QDrantQueriesCacheItem
    {
        public const string AgentMemoryQueryKind = "AgentMemory";
        public const string SemanticQueryKind = "SemanticQuery";
        public const string KeywordsQueryKind = "Keywords Query";
        public const string HydeQueryKind = "HydeQuery";

        public string Query { get; set; } = string.Empty;
        public string QueryKind { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public KnowledgeBaseQuerySearchType? QueryType { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
        public string? DocumentSummary { get; set; }
        public string DocumentFile { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
        public double Relevance { get; set; }
    }
}
