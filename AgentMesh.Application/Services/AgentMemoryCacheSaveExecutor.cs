using AgentMesh.Application.Contracts;
using AgentMesh.Models.AgentMemoryCacheSave;
using AgentMesh.Models.DocumentsCache;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class AgentMemoryCacheSaveExecutor : IAgentMemoryCacheSaveExecutor
    {
        private readonly IDocumentsCacheService _documentsCacheService;

        public AgentMemoryCacheSaveExecutor(IDocumentsCacheService documentsCacheService)
        {
            _documentsCacheService = documentsCacheService;
        }

        public async Task<AgentMemoryCacheSaveOutput> ExecuteAsync(AgentMemoryCacheSaveInput input, CancellationToken cancellationToken = default)
        {
            await _documentsCacheService.SaveAgentMemory(input.AgentMemoryCachedQueries, input.AgentMemoryCachedQueryResult);
            return new AgentMemoryCacheSaveOutput { Success = true };
        }
    }
}
