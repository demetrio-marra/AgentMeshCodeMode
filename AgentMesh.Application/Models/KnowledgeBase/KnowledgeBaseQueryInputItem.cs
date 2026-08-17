using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Models.KnowledgeBase
{
    public class KnowledgeBaseQueryInputItem
    {
        public string Query { get; set; } = string.Empty;
        public KnowledgeBaseQuerySearchType SearchType { get; set; }

        public override string ToString()
        {
            return $"{Query} [{SearchType}]";
        }
    }
}
