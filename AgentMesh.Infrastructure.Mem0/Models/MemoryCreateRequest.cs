namespace AgentMesh.Infrastructure.Mem0.Models
{
    public class MemoryCreateRequest
    {
        public List<Message> Messages { get; set; } = [];
        public string? UserId { get; set; }
        public string? AgentId { get; set; }
        public string? RunId { get; set; }
        public object? Metadata { get; set; }
    }
}
