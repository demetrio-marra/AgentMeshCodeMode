namespace AgentMesh.Application.Models.Knowledge
{
    public class KnowledgeRerankerResult
    {
        public IEnumerable<string> EntityIds { get; set; } = [];
        public IEnumerable<string> RelationIds { get; set; } = [];
        public IEnumerable<string> ContentIds { get; set; } = [];
    }
}
