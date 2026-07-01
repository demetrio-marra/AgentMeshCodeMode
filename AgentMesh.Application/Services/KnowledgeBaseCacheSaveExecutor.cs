using AgentMesh.Application.Contracts;
using AgentMesh.Models.DocumentsCache;
using AgentMesh.Models.KnowledgeBaseCacheSave;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class KnowledgeBaseCacheSaveExecutor : IKnowledgeBaseCacheSaveExecutor
    {
        private readonly IDocumentsCacheService _documentsCacheService;

        public KnowledgeBaseCacheSaveExecutor(IDocumentsCacheService documentsCacheService)
        {
            _documentsCacheService = documentsCacheService;
        }

        public async Task<KnowledgeBaseCacheSaveOutput> ExecuteAsync(KnowledgeBaseCacheSaveInput input, CancellationToken cancellationToken = default)
        {
            await _documentsCacheService.SaveKnowledgeBase(input.KnowledgeBaseCachedQueries, input.KnowledgeBaseCachedQueryResult);
            return new KnowledgeBaseCacheSaveOutput { Success = true };
        }
    }
}
