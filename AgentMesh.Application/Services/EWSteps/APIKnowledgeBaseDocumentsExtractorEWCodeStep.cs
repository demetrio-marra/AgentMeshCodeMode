using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class APIKnowledgeBaseDocumentsExtractorEWCodeStep(
        KnowledgeBaseExecutor knowledgeBaseExecutor,
        APISKnowledgeBaseQueryResultsParameter apisKnowledgeBaseQueryResultsParameter,
        KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter) : IEWCodeStep
    {
        public string Name => "API Knowledge Base Documents Extractor";

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var results = apisKnowledgeBaseQueryResultsParameter.ParameterValue ?? [];
            var filesToExtract = results
                .Select(r => r.File?.Trim())
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();

            var fetchedFilesContent = await knowledgeBaseExecutor.GetDocsAsync(new KnowledgeBaseGetDocsInput
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

            knowledgeBaseAPIDocumentsContentParameter.ParameterValue = documents;
        }
    }
}
