using AgentMesh.Models.Reranker;

namespace AgentMesh.Services
{
    public interface IRerankerAgent : IExecutor<RerankerAgentInput, RerankerAgentOutput>
    {
    }
}
