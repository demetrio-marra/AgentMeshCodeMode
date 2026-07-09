using AgentMesh.Services;
using AgentMesh.Application.Models;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Services.Executors;

public class DomainsKnowledgeBaseDocumentsExtractorWorkflowExecutor(
    KnowledgeBaseDocumentsExtractorWorkflowExecutor knowledgeBaseDocumentsExtractorWorkflowExecutor)
{
    private readonly KnowledgeBaseDocumentsExtractorWorkflowExecutor _knowledgeBaseDocumentsExtractorWorkflowExecutor = knowledgeBaseDocumentsExtractorWorkflowExecutor;

    public async Task ExecuteDomainsKnowledgeBaseDocumentsExtractorAsync(CodeModeWorkflowState state)
    {
        await _knowledgeBaseDocumentsExtractorWorkflowExecutor.ExecuteKnowledgeBaseDocumentsExtractorAsync(
            state,
            "Engaging Knowledge Base Documents Extractor Service...",
            "KB Documents Extractor Service (Domain)",
            "Documents",
            workflowState => workflowState.DomainsKnowledgeBaseQueryResults.Results.Select(r => r.File),
            file => file?.Trim() ?? string.Empty,
            StringComparer.Ordinal,
            results => results
                .Where(doc => !string.IsNullOrWhiteSpace(doc.File))
                .GroupBy(doc => doc.File!)
                .ToDictionary(
                    group => group.Key,
                    group => new KnowledgeBaseDocumentContent
                    {
                        File = group.Key,
                        Content = group.First().Content
                    }),
            (workflowState, documents) => workflowState.DomainsKnowledgeBaseDocumentsContent = documents);
    }
}

