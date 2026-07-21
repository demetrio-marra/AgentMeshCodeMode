using AgentMesh.Application.Models;

namespace AgentMesh.Application.Workflows.Steps;

public class APIsKnowledgeBaseServiceSearchWorkflowStep(
    KnowledgeBaseServiceSearchWorkflowStep knowledgeBaseServiceSearchWorkflowExecutor)
{
    private const string APIsDocumentationCollectionName = "apis";

    private readonly KnowledgeBaseServiceSearchWorkflowStep _knowledgeBaseServiceSearchWorkflowExecutor = knowledgeBaseServiceSearchWorkflowExecutor;

    public async Task ExecuteAPIsKnowledgeBaseServiceSearchAsync(CodeModeWorkflowState state)
    {
        var apiQueries = state.DomainsKnowledgeBaseQuery.ToList();

        await _knowledgeBaseServiceSearchWorkflowExecutor.ExecuteKnowledgeBaseServiceSearchAsync(
            state,
            "APIs Knowledge Base Service",
            APIsDocumentationCollectionName,
            workflowState => apiQueries,
            workflowState => workflowState.APISKnowledgeBaseQueryResults,
            (workflowState, queryResult) => workflowState.APISKnowledgeBaseQueryResults = queryResult);
    }
}

