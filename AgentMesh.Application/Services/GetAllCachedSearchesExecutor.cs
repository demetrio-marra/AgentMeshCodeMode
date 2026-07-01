using AgentMesh.Application.Contracts;
using AgentMesh.Models.DocumentsCache;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class GetAllCachedSearchesExecutor : IGetAllCachedSearchesExecutor
    {
        private readonly IDocumentsCacheService _documentsCacheService;

        public GetAllCachedSearchesExecutor(IDocumentsCacheService documentsCacheService)
        {
            _documentsCacheService = documentsCacheService;
        }

        public async Task<GetAllCachedSearchesExecutorOutput> ExecuteAsync(GetAllCachedSearchesExecutorInput input, CancellationToken cancellationToken = default)
        {
            var (agentMemoryQueries, knowledgeBaseQueries) = await _documentsCacheService.GetAllCachedSearchesAsync();
            
            return new GetAllCachedSearchesExecutorOutput
            {
                AgentMemoryCachedQueries = agentMemoryQueries,
                KnowledgeBaseCachedQueries = knowledgeBaseQueries
            };
        }
    }
}
