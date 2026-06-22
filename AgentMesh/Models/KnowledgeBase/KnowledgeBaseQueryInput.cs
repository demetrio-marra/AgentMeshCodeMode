namespace AgentMesh.Models.KnowledgeBase
{
    public class KnowledgeBaseQueryInput
    {
        public IEnumerable<string> Queries { get; set; } = [];
        public IEnumerable<string> Collections { get; set; } = [];
        public KnowledgeBaseQuerySearchType SearchType { get; set; }
    }

    public enum KnowledgeBaseQuerySearchType
    {
        KeywordsOnly,
        SemanticOnly,
        Full
    }
}
