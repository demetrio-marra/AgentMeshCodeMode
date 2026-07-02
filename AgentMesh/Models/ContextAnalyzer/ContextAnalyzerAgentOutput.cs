namespace AgentMesh.Models.ContextAnalyzer
{
    public class ContextAnalyzerAgentOutput : IAgentOutput
    {
        public string CondensedUserIntent { get; set; } = string.Empty;
        public UserIntentCategoryValues UserIntentCategory { get; set; }
        public IEnumerable<FilteredKnowledgeBaseItem> FilteredKnowledgeBaseDocuments { get; set; } = [];

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public enum UserIntentCategoryValues
        {
            Other,
            BusinessAdvisor,
            Documentation,
            TaskExecution
        }

        public class FilteredKnowledgeBaseItem
        {
            public string Title { get; set; } = string.Empty;
            public string DocumentId { get; set; } = string.Empty;
        }
    }
}
