namespace AgentMesh.Application.Models.KnowledgeBase
{
    public class KnowledgeBaseQueryInput
    {
        public IEnumerable<string> Collections { get; set; } = [];
        public IEnumerable<KnowledgeBaseQueryInputItem> Queries { get; set; } = [];
        public string? UserIntent { get; set; }
    }
}
