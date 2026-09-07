using AgentMesh.Application.Models.Knowledge;
using System.Threading;

namespace AgentMesh.Application.Contracts
{
    public interface IKnowledgeService
    {
        Task<KnowledgeQueryResult> QueryKnowledgeAsync(KnowledgeQuery query, CancellationToken cancellationToken = default);
    }
}
