using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Services
{
    public interface IKnowledgeBaseSearchFastExecutor : IExecutor<KnowledgeBaseQueryInput, KnowledgeBaseQueryResult>
    {
    }
}
