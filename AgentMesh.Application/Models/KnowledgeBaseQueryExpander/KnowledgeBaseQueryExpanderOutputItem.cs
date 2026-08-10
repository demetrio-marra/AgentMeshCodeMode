using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Models.KnowledgeBaseQueryExpander
{
    public readonly record struct KnowledgeBaseQueryExpanderOutputItem
    {
        public string Query { get; init; } 
        public KnowledgeBaseQuerySearchType SearchType { get; init; }
    }
}
