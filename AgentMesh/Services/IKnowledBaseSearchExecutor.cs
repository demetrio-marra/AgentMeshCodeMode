using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Services
{
    public interface IKnowledgeBaseSearchExecutor : IExecutor<KnowledgeBaseQueryInput, KnowledgeBaseQueryResult>
    {
    }
}
