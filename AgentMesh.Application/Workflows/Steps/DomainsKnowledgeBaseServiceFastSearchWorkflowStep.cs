using AgentMesh.Application.Models;
using AgentMesh.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Workflows.Steps;

public class DomainsKnowledgeBaseServiceFastSearchWorkflowStep(
    KnowledgeBaseServiceFastSearchWorkflowStep knowledgeBaseServiceFastSearchWorkflowExecutor)
{
    private const string DomainsDocumentationCollectionName = "domains";

    private readonly KnowledgeBaseServiceFastSearchWorkflowStep _knowledgeBaseServiceFastSearchWorkflowExecutor = knowledgeBaseServiceFastSearchWorkflowExecutor;

    public async Task ExecuteDomainsKnowledgeBaseServiceFastSearchAsync(CodeModeWorkflowState state)
    {
        await _knowledgeBaseServiceFastSearchWorkflowExecutor.ExecuteKnowledgeBaseServiceFastSearchAsync(
            state,
            "Engaging Knowledge Base Fast Service...",
            "No domains or entities to search for in knowledge base",
            "KB Fast Search Service",
            "Domains",
            workflowState => WorkflowExecutorFormatting.ToBulletList(workflowState.UserProvidedData),
            "ExtractedKnowledgeBaseEntries",
            "FastKnowledgeBaseQueryResults",
            DomainsDocumentationCollectionName,
            workflowState => workflowState.UserProvidedData
                .Select(entry => new KnowledgeBaseQueryInputItem
                {
                    Query = entry,
                    SearchType = KnowledgeBaseQuerySearchType.Keyword
                }),
            (workflowState, queryResult) => workflowState.FastDomainsKnowledgeBaseQueryResults = queryResult);
    }
}

