using AgentMesh.Application.Models.Rerank;

namespace AgentMesh.Application.Contracts
{
    public interface IRerankerService
    {
        Task<RerankResult> RerankAsync(RerankInputQuery inputQuery, CancellationToken cancellationToken = default);
    }
}
