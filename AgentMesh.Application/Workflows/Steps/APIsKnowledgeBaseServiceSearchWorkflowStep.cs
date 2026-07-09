using AgentMesh.Application.Models;
using AgentMesh.Services;
using AgentMesh.Application.Workflows;

namespace AgentMesh.Application.Workflows.Steps;

public class APIsKnowledgeBaseServiceSearchWorkflowStep(
    KnowledgeBaseServiceSearchWorkflowStep knowledgeBaseServiceSearchWorkflowExecutor)
{
    private const string APIsDocumentationCollectionName = "apis";

    private readonly KnowledgeBaseServiceSearchWorkflowStep _knowledgeBaseServiceSearchWorkflowExecutor = knowledgeBaseServiceSearchWorkflowExecutor;

    public async Task ExecuteAPIsKnowledgeBaseServiceSearchAsync(CodeModeWorkflowState state)
    {
        await _knowledgeBaseServiceSearchWorkflowExecutor.ExecuteKnowledgeBaseServiceSearchAsync(
            state,
            "APIs Knowledge Base Service",
            APIsDocumentationCollectionName,
            workflowState => workflowState.CanonicalizedAPIQueries,
            workflowState => workflowState.APISKnowledgeBaseQueryResults,
            (workflowState, queryResult) => workflowState.APISKnowledgeBaseQueryResults = queryResult);
    }
}

