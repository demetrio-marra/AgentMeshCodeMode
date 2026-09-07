namespace AgentMesh.Application.Models.Knowledge
{
    public class KnowledgeQueryResult
    {
        public IEnumerable<KnowledgeContentItem> Contents { get; set; } = [];
        public IEnumerable<KnowledgeEntityItem> Entities { get; set; } = [];
        public IEnumerable<KnowledgeRelationItem> Relations { get; set; } = [];
    }
}
