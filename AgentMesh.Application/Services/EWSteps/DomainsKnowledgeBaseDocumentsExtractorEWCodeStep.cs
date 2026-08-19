using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainsKnowledgeBaseDocumentsExtractorEWCodeStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter) : IEWStep
    {
        public string Name => "Domains Knowledge Base Documents Extractor";

        public IEnumerable<Type> InputParameterTypes => [typeof(KnowledgeBaseQueryResultsParameter)];

        public IEnumerable<Type> OutputParameterTypes => [typeof(DomainsKnowledgeBaseDocumentsContentParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var results = knowledgeBaseQueryResultsParameter.ValueAs(Values[typeof(KnowledgeBaseQueryResultsParameter)]) ?? [];
            var filesToExtract = results
                .Select(r => r.File?.Trim())
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .Distinct(StringComparer.Ordinal)
                .Cast<string>()
                .ToList();

            var fetchedFilesContent = await knowledgeBaseExecutor.GetDocsAsync(new KnowledgeBaseGetDocsInput
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

            return new EWStepExecutionResult
            {
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(DomainsKnowledgeBaseDocumentsContentParameter), documents }
                }
            };
        }
    }
}
