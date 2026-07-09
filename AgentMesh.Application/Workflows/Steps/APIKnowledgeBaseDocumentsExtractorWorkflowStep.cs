using AgentMesh.Services;
using AgentMesh.Application.Models;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Workflows.Steps;

public class APIKnowledgeBaseDocumentsExtractorWorkflowStep(
    KnowledgeBaseDocumentsExtractorWorkflowStep knowledgeBaseDocumentsExtractorWorkflowExecutor)
{
    private readonly KnowledgeBaseDocumentsExtractorWorkflowStep _knowledgeBaseDocumentsExtractorWorkflowExecutor = knowledgeBaseDocumentsExtractorWorkflowExecutor;

    public async Task ExecuteAPIKnowledgeBaseDocumentsExtractorAsync(CodeModeWorkflowState state)
    {
        await _knowledgeBaseDocumentsExtractorWorkflowExecutor.ExecuteKnowledgeBaseDocumentsExtractorAsync(
            state,
            "Engaging Knowledge Base API Documents Extractor Service...",
            "KB Documents Extractor Service (APIs)",
            "Documents",
            workflowState => workflowState.APISKnowledgeBaseQueryResults.Results.Select(r => r.File),
            file => file?.Trim() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase,
            results => results
                .Where(doc => !string.IsNullOrWhiteSpace(doc.File))
                .GroupBy(doc => doc.File!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => new KnowledgeBaseDocumentContent
                    {
                        File = group.Key,
                        Content = group.First().Content
                    },
                    StringComparer.OrdinalIgnoreCase),
            (workflowState, documents) => workflowState.KnowledgeBaseAPIDocumentsContent = documents);
    }
}

