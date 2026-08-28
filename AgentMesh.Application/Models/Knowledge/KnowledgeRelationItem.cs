namespace AgentMesh.Application.Models.Knowledge
{
    public class KnowledgeRelationItem
    {
        public string Description { get; set; } = string.Empty;

        public string Keywords { get; set; } = string.Empty;

        public KnowledgeContentItem ContentItem { get; set; } = new();

        public string EntityRelationFrom { get; set; } = string.Empty;

        public string EntityRelationTo { get; set; } = string.Empty;
    }
}
