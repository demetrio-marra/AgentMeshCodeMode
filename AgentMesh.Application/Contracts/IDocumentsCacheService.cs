using AgentMesh.Models.DocumentsCache;

namespace AgentMesh.Application.Contracts
{
    public interface IDocumentsCacheService
    {
        Task<Tuple<AgentMemoryCachedQueryResult?, KnowledgeBaseCachedQueryResult?>> ExecuteDocumentsCacheQueryAsync(AgentMemoryCachedQuery? agentMemoryCachedQuery, KnowledgeBaseCachedQuery? knowledgeBaseCachedQuery);
        Task SaveAgentMemory(AgentMemoryCachedQuery? agentMemoryCachedQuery, AgentMemoryCachedQueryResult? agentMemoryCachedQueryResult);
        Task SaveKnowledgeBase(KnowledgeBaseCachedQuery? knowledgeBaseCachedQuery, KnowledgeBaseCachedQueryResult? knowledgeBaseCachedQueryResult);
        Task<Tuple<IEnumerable<AgentMemoryCachedQuery>, IEnumerable<KnowledgeBaseCachedQuery>>> GetAllCachedSearchesAsync();
    }
}
