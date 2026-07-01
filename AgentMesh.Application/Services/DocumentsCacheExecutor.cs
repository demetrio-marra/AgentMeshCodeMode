using AgentMesh.Application.Contracts;
using AgentMesh.Models.DocumentsCache;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class DocumentsCacheExecutor : IDocumentsCacheExecutor
    {
        private readonly IDocumentsCacheService _documentsCacheService;

        public DocumentsCacheExecutor(IDocumentsCacheService documentsCacheService)
        {
            _documentsCacheService = documentsCacheService;
        }

        public async Task<DocumentsCacheExecutorOutput> ExecuteAsync(DocumentsCacheExecutorInput input, CancellationToken cancellationToken = default)
        {
            var docs = await _documentsCacheService.ExecuteDocumentsCacheQueryAsync(input.AgentMemoryCachedQuery, input.KnowledgeBaseCachedQuery);
            var ret = new DocumentsCacheExecutorOutput
            {
                AgentMemoryCachedQueryResult = docs.Item1,
                KnowledgeBaseCachedQueryResult = docs.Item2
            };
            return ret;
        }
    }
}
