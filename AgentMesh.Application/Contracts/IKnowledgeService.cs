using AgentMesh.Application.Models.Knowledge;

namespace AgentMesh.Application.Contracts
{
    public interface IKnowledgeService
    {
        Task<KnowledgeQueryResult> QueryKnowledgeAsync(KnowledgeQuery query);
    }
}
