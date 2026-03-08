namespace AgentMesh.Infrastructure.AgentMemory.Models
{
    public class SearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? RunId { get; set; }
        public string? AgentId { get; set; }
        public object? Filters { get; set; }
    }
}
