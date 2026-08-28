namespace AgentMesh.Application.Models.Knowledge
{
    public class KnowledgeQuery
    {
        public string Query { get; set; } = string.Empty;
        public IEnumerable<string> PrimaryRelevanceKeywords { get; set; } = [];
        public IEnumerable<string> SecondaryRelevanceKeywords { get; set; } = [];
        public int MaxResults { get; set; } = 10;
        public bool IncludeEntities { get; set; } = true;
        public bool IncludeRelations { get; set; } = true;
    }
}
