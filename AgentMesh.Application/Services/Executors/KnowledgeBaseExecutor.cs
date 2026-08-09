using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Services.Executors
{
    public class KnowledgeBaseExecutor(
        IKnowledgeBaseService knowledgeBaseService,
        CodeModeWorkflowConfiguration codeModeWorkflowConfiguration)
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService = knowledgeBaseService;
        private readonly CodeModeWorkflowConfiguration _codeModeWorkflowConfiguration = codeModeWorkflowConfiguration;

        public async Task<KnowledgeBaseGetDocsOutput> GetDocsAsync(KnowledgeBaseGetDocsInput input, CancellationToken cancellationToken)
        {
            if (input.FilePaths == null || !input.FilePaths.Any())
            {
                throw new ArgumentException("FilePaths cannot be null or empty.", nameof(input.FilePaths));
            }

            var results = await _knowledgeBaseService.GetKnowledgeBaseEntriesContentAsync(input.FilePaths, cancellationToken);
            return new KnowledgeBaseGetDocsOutput { Results = [.. results.Select(r => new KnowledgeBaseGetDocsOutputItem { File = r.File, Content = r.Content })] };
        }

        public async Task<KnowledgeBaseQueryResult> QueryAsync(KnowledgeBaseQueryInput input, CancellationToken cancellationToken)
        {
            if (input == null)
            {
                throw new ArgumentException("Input cannot be null or empty.", nameof(input));
            }

            var result = await _knowledgeBaseService.FindAsync(input, _codeModeWorkflowConfiguration.RerankOnHybridSearch, cancellationToken);

            return new KnowledgeBaseQueryResult
            {
                Results = [.. result.Results.Select(r => new KnowledgeBaseQueryResultItem
                {
                    File = r.File,
                    Id = r.Id,
                    Relevance = r.Relevance,
                    Summary = r.Summary,
                    Title = r.Title
                })]
            };
        }
    }
}
