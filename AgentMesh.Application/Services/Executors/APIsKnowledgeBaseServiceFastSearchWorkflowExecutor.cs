using AgentMesh.Application.Models;
using AgentMesh.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Services.Executors;

public class APIsKnowledgeBaseServiceFastSearchWorkflowExecutor(
    KnowledgeBaseServiceFastSearchWorkflowExecutor knowledgeBaseServiceFastSearchWorkflowExecutor)
{
    private const string APIsDocumentationCollectionName = "apis";

    private readonly KnowledgeBaseServiceFastSearchWorkflowExecutor _knowledgeBaseServiceFastSearchWorkflowExecutor = knowledgeBaseServiceFastSearchWorkflowExecutor;

    public async Task ExecuteAPIsKnowledgeBaseServiceFastSearchAsync(CodeModeWorkflowState state)
    {
        await _knowledgeBaseServiceFastSearchWorkflowExecutor.ExecuteKnowledgeBaseServiceFastSearchAsync(
            state,
            "Engaging Knowledge Base Fast Service for APIs...",
            "No APIs to search for in knowledge base",
            "API Fast Search Service",
            "APIs",
            workflowState => WorkflowExecutorFormatting.ToBulletList(workflowState.FastAPISKnowledgeBaseQuery),
            "FastAPISKnowledgeBaseQueryResults",
            "FastAPISKnowledgeBaseQueryResults",
            APIsDocumentationCollectionName,
            workflowState => workflowState.FastAPISKnowledgeBaseQuery
                .Where(query => !string.IsNullOrWhiteSpace(query))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(query => new KnowledgeBaseQueryInputItem
                {
                    Query = query,
                    SearchType = KnowledgeBaseQuerySearchType.Keyword
                }),
            (workflowState, queryResult) => workflowState.FastAPISKnowledgeBaseQueryResults = queryResult);
    }
}

