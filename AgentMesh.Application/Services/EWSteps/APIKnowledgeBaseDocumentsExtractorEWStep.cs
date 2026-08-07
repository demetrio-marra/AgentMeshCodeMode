using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class APIKnowledgeBaseDocumentsExtractorEWStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        APISKnowledgeBaseQueryResultsParameter apisKnowledgeBaseQueryResultsParameter,
        KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter) : IEWStep
    {
        public string Name => "API Knowledge Base Documents Extractor";

        public bool IsAgentic => false;

        public string? AgentName => null;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly KnowledgeBaseExecutor _knowledgeBaseExecutor = knowledgeBaseExecutor;
        private readonly APISKnowledgeBaseQueryResultsParameter _apisKnowledgeBaseQueryResultsParameter = apisKnowledgeBaseQueryResultsParameter;
        private readonly KnowledgeBaseAPIDocumentsContentParameter _knowledgeBaseAPIDocumentsContentParameter = knowledgeBaseAPIDocumentsContentParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var results = _apisKnowledgeBaseQueryResultsParameter.ParameterValue ?? [];
            var filesToExtract = results
                .Select(r => r.File?.Trim())
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();

            var fetchedFilesContent = await _knowledgeBaseExecutor.GetDocsAsync(new KnowledgeBaseGetDocsInput
            {
                FilePaths = filesToExtract
            }, cancellationToken);

            var documentsByFile = fetchedFilesContent.Results
                .Where(doc => !string.IsNullOrWhiteSpace(doc.File))
                .GroupBy(doc => doc.File!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => new KnowledgeBaseDocumentContent
                    {
                        File = group.Key,
                        Content = group.First().Content
                    },
                    StringComparer.OrdinalIgnoreCase);

            var documents = documentsByFile.Values.ToList();

            _knowledgeBaseAPIDocumentsContentParameter.ParameterValue = documents;

            return new EWStepResultRecord(null, null);
        }
    }
}
