using AgentMesh.Application.Models;
using AgentMesh.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Services.Executors;

public class DomainsKnowledgeBaseServiceFastSearchWorkflowExecutor(
    KnowledgeBaseServiceFastSearchWorkflowExecutor knowledgeBaseServiceFastSearchWorkflowExecutor)
{
    private const string DomainsDocumentationCollectionName = "domains";

    private readonly KnowledgeBaseServiceFastSearchWorkflowExecutor _knowledgeBaseServiceFastSearchWorkflowExecutor = knowledgeBaseServiceFastSearchWorkflowExecutor;

    public async Task ExecuteDomainsKnowledgeBaseServiceFastSearchAsync(CodeModeWorkflowState state)
    {
        await _knowledgeBaseServiceFastSearchWorkflowExecutor.ExecuteKnowledgeBaseServiceFastSearchAsync(
            state,
            "Engaging Knowledge Base Fast Service...",
            "No domains or entities to search for in knowledge base",
            "KB Fast Search Service",
            "Domains",
            workflowState => WorkflowExecutorFormatting.ToBulletList(workflowState.ClassifiedUserRequest.EntitiesByDomain.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}")),
            "ExtractedKnowledgeBaseEntries",
            "FastKnowledgeBaseQueryResults",
            DomainsDocumentationCollectionName,
            workflowState => workflowState.ClassifiedUserRequest.EntitiesByDomain
                .SelectMany(domainEntry =>
                    new[] { domainEntry.Key }
                        .Concat(domainEntry.Value)
                        .Select(entry => new KnowledgeBaseQueryInputItem
                        {
                            Query = entry,
                            SearchType = KnowledgeBaseQuerySearchType.Keyword
                        })),
            (workflowState, queryResult) => workflowState.FastDomainsKnowledgeBaseQueryResults = queryResult);
    }
}

