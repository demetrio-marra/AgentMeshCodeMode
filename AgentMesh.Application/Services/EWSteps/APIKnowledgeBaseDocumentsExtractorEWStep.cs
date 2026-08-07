using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class APIKnowledgeBaseDocumentsExtractorEWStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "API Knowledge Base Documents Extractor";

        public bool IsAgentic => false;

        public string? AgentName => null;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.APISKnowledgeBaseQueryResults
        ];

        private readonly KnowledgeBaseExecutor _knowledgeBaseExecutor = knowledgeBaseExecutor;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var queryResultsParameter = inputParameters.Single(p => p.Name == EWParameterNames.APISKnowledgeBaseQueryResults);
            if (queryResultsParameter is not APISKnowledgeBaseQueryResultsParameter typedQueryResults)
                throw new InvalidOperationException($"Parameter {EWParameterNames.APISKnowledgeBaseQueryResults} is not of type APISKnowledgeBaseQueryResultsParameter");

            var results = typedQueryResults.ParameterValue ?? [];
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

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.KnowledgeBaseAPIDocumentsContent, (IEnumerable<KnowledgeBaseDocumentContent>)documents);

            return new EWStepResultRecord(null, null);
        }
    }
}
