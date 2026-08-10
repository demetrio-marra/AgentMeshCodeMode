using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainsKnowledgeBaseDocumentsExtractorEWCodeStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter,
        DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter) : IEWCodeStep
    {
        public string Name => "Domains Knowledge Base Documents Extractor";
        
        public bool IsContextAnalyzerStep => false;

        public bool IsLastPipelineStep => false;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var results = knowledgeBaseQueryResultsParameter.ParameterValue ?? [];
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

            domainsKnowledgeBaseDocumentsContentParameter.ParameterValue = documents;
        }
    }
}
