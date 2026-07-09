using AgentMesh.Application.Models;
using AgentMesh.Services;
using AgentMesh.Application.Workflows;

namespace AgentMesh.Application.Services.Executors;

public class DomainsKnowledgeBaseServiceSearchWorkflowExecutor(
    KnowledgeBaseServiceSearchWorkflowExecutor knowledgeBaseServiceSearchWorkflowExecutor)
{
    private const string DomainsDocumentationCollectionName = "domains";

    private readonly KnowledgeBaseServiceSearchWorkflowExecutor _knowledgeBaseServiceSearchWorkflowExecutor = knowledgeBaseServiceSearchWorkflowExecutor;

    public async Task ExecuteDomainsKnowledgeBaseServiceSearchAsync(CodeModeWorkflowState state)
    {
        await _knowledgeBaseServiceSearchWorkflowExecutor.ExecuteKnowledgeBaseServiceSearchAsync(
            state,
            "KB Search Service",
            DomainsDocumentationCollectionName,
            workflowState => workflowState.DomainsKnowledgeBaseQuery,
            workflowState => workflowState.DomainsKnowledgeBaseQueryResults,
            (workflowState, queryResult) => workflowState.DomainsKnowledgeBaseQueryResults = queryResult);
    }
}

