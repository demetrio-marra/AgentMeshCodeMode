using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainsKnowledgeBaseDocumentsExtractorEWStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Domains Knowledge Base Documents Extractor";

        public bool IsAgentic => false;

        public string? AgentName => null;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.KnowledgeBaseQueryResults
        ];

        private readonly KnowledgeBaseExecutor _knowledgeBaseExecutor = knowledgeBaseExecutor;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var queryResultsParameter = inputParameters.Single(p => p.Name == EWParameterNames.KnowledgeBaseQueryResults);
            if (queryResultsParameter is not KnowledgeBaseQueryResultsParameter typedQueryResults)
                throw new InvalidOperationException($"Parameter {EWParameterNames.KnowledgeBaseQueryResults} is not of type KnowledgeBaseQueryResultsParameter");

            var results = typedQueryResults.ParameterValue ?? [];
            var filesToExtract = results
                .Select(r => r.File?.Trim())
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .Distinct(StringComparer.Ordinal)
                .Cast<string>()
                .ToList();

            var fetchedFilesContent = await _knowledgeBaseExecutor.GetDocsAsync(new KnowledgeBaseGetDocsInput
            {
                FilePaths = filesToExtract
            }, cancellationToken);

            var documentsByFile = fetchedFilesContent.Results
                .Where(doc => !string.IsNullOrWhiteSpace(doc.File))
                .GroupBy(doc => doc.File!)
                .ToDictionary(
                    group => group.Key,
                    group => new KnowledgeBaseDocumentContent
                    {
                        File = group.Key,
                        Content = group.First().Content
                    });

            var documents = documentsByFile.Values.ToList();

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.DomainsKnowledgeBaseDocumentsContent, (IEnumerable<KnowledgeBaseDocumentContent>)documents);

            return new EWStepResultRecord(null, null);
        }
    }
}
