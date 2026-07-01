using AgentMesh.Application.Contracts;
using AgentMesh.Models.DocumentsCache;

namespace AgentMesh.Infrastructure.DocumentsCache
{
    public class DummyDocumentsCacheService : IDocumentsCacheService
    {
        private readonly Dictionary<AgentMemoryCachedQuery, AgentMemoryCachedQueryResult> _agentMemoryCache = new();
        private readonly Dictionary<KnowledgeBaseCachedQuery, KnowledgeBaseCachedQueryResult> _knowledgeBaseCache = new();

        public async Task<Tuple<AgentMemoryCachedQueryResult?, KnowledgeBaseCachedQueryResult?>> ExecuteDocumentsCacheQueryAsync(AgentMemoryCachedQuery? agentMemoryCachedQuery, 
            KnowledgeBaseCachedQuery? knowledgeBaseCachedQuery)
        {
            AgentMemoryCachedQueryResult? agentMemoryResult = null;
            KnowledgeBaseCachedQueryResult? knowledgeBaseResult = null;

            if (agentMemoryCachedQuery != null && _agentMemoryCache.TryGetValue(agentMemoryCachedQuery, out var cachedAgentMemory))
            {
                agentMemoryResult = cachedAgentMemory;
            }

            if (knowledgeBaseCachedQuery != null && _knowledgeBaseCache.TryGetValue(knowledgeBaseCachedQuery, out var cachedKnowledgeBase))
            {
                knowledgeBaseResult = cachedKnowledgeBase;
            }

            return await Task.FromResult(new Tuple<AgentMemoryCachedQueryResult?, KnowledgeBaseCachedQueryResult?>(agentMemoryResult, knowledgeBaseResult));
        }

        public Task SaveAgentMemory(AgentMemoryCachedQuery? agentMemoryCachedQuery, AgentMemoryCachedQueryResult? agentMemoryCachedQueryResult)
        {
            if (agentMemoryCachedQuery != null && agentMemoryCachedQueryResult != null)
            {
                _agentMemoryCache[agentMemoryCachedQuery] = agentMemoryCachedQueryResult;
            }

            return Task.CompletedTask;
        }

        public Task SaveKnowledgeBase(KnowledgeBaseCachedQuery? knowledgeBaseCachedQuery, KnowledgeBaseCachedQueryResult? knowledgeBaseCachedQueryResult)
        {
            if (knowledgeBaseCachedQuery != null && knowledgeBaseCachedQueryResult != null)
            {
                _knowledgeBaseCache[knowledgeBaseCachedQuery] = knowledgeBaseCachedQueryResult;
            }

            return Task.CompletedTask;
        }

        public Task<Tuple<IEnumerable<AgentMemoryCachedQuery>, IEnumerable<KnowledgeBaseCachedQuery>>> GetAllCachedSearchesAsync()
        {
            return Task.FromResult(new Tuple<IEnumerable<AgentMemoryCachedQuery>, IEnumerable<KnowledgeBaseCachedQuery>>(
                _agentMemoryCache.Keys,
                _knowledgeBaseCache.Keys));
        }
    }
}
