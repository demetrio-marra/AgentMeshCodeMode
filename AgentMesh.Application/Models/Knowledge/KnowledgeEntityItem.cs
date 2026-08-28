namespace AgentMesh.Application.Models.Knowledge
{
    public class KnowledgeEntityItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Entity { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public KnowledgeContentItem ContentItem { get; set; } = new();
    }
}
