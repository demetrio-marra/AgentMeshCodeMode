using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.DocumentsCache;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Contracts
{
    public interface IDocumentsCacheService
    {
        Task<Tuple<AgentMemoryQueryResult?, KnowledgeBaseQueryResult?>> ExecuteDocumentsCacheQueryAsync(
            IEnumerable<AgentMemoryCacheableQuery>? agentMemoryCachedQueries,
            IEnumerable<KnowledgeBaseCacheableQuery>? knowledgeBaseCachedQueries);

        Task SaveAgentMemory(IEnumerable<AgentMemoryCacheableQuery>? agentMemoryCachedQueries, AgentMemoryQueryResult agentMemoryQueryResults);

        Task SaveKnowledgeBase(IEnumerable<KnowledgeBaseCacheableQuery> knowledgeBaseCachedQueries, KnowledgeBaseQueryResult knowledgeBaseQueryResults);

        Task<Tuple<IEnumerable<AgentMemoryCacheableQuery>, IEnumerable<KnowledgeBaseCacheableQuery>>> GetAllCachedSearchesAsync();
    }
}
