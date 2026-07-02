namespace AgentMesh.Infrastructure.Mem0.Models
{
    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string Memory { get; set; } = string.Empty;
        public float Score { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
