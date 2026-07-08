using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class KnowledgeBaseExecutor(
        IKnowledgeBaseService knowledgeBaseService,
        CodeModeWorkflowConfiguration codeModeWorkflowConfiguration) : IKnowledgeBaseSearchExecutor, IKnowledgeBaseGetDocsExecutor
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService = knowledgeBaseService;
        private readonly CodeModeWorkflowConfiguration _codeModeWorkflowConfiguration = codeModeWorkflowConfiguration;

        async Task<KnowledgeBaseGetDocsOutput> IExecutor<KnowledgeBaseGetDocsInput, KnowledgeBaseGetDocsOutput>.ExecuteAsync(KnowledgeBaseGetDocsInput input, CancellationToken cancellationToken)
        {
            if (input.FilePaths == null || !input.FilePaths.Any())
            {
                throw new ArgumentException("FilePaths cannot be null or empty.", nameof(input.FilePaths));
            }

            var results = await _knowledgeBaseService.GetKnowledgeBaseEntriesContentAsync(input.FilePaths, cancellationToken);
            return new KnowledgeBaseGetDocsOutput { Results = [.. results.Select(r => new KnowledgeBaseGetDocsOutputItem { File = r.File, Content = r.Content })] };
        }


        async Task<KnowledgeBaseQueryResult> IExecutor<KnowledgeBaseQueryInput, KnowledgeBaseQueryResult>.ExecuteAsync(KnowledgeBaseQueryInput input, CancellationToken cancellationToken)
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
