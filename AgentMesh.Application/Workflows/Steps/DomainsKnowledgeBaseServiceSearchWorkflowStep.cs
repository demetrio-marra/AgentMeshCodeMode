using AgentMesh.Application.Models;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.Workflows;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class DomainsKnowledgeBaseServiceSearchWorkflowStep(
    KnowledgeBaseServiceSearchWorkflowStep knowledgeBaseServiceSearchWorkflowExecutor) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string DomainsDocumentationCollectionName = "domains";
    private const string WorkflowStepDisplayName = "Domains Knowledge Base Service Search";

    private readonly KnowledgeBaseServiceSearchWorkflowStep _knowledgeBaseServiceSearchWorkflowExecutor = knowledgeBaseServiceSearchWorkflowExecutor;

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

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteDomainsKnowledgeBaseServiceSearchAsync(stateObject);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

