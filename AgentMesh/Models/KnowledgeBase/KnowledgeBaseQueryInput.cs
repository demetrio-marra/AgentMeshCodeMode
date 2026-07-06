namespace AgentMesh.Models.KnowledgeBase
{
    public class KnowledgeBaseQueryInput
    {
        public IEnumerable<string> Collections { get; set; } = [];
        public IEnumerable<KnowledgeBaseQueryInputItem> Queries { get; set; } = [];
        public string? UserIntent { get; set; }
    }

    public class KnowledgeBaseQueryInputItem
    {
        public string Query { get; set; } = string.Empty;
        public KnowledgeBaseQuerySearchType SearchType { get; set; }

        public override string ToString()
        {
            return $"Query: {Query}, SearchType: {SearchType}";
        }
    }

    public enum KnowledgeBaseQuerySearchType
    {
        Keyword,
        Semantic,
        HypotheticalDocument

    }
}
