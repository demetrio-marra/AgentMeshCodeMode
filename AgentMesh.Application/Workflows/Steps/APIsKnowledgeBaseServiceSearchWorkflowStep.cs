using AgentMesh.Application.Models;
using AgentMesh.Models.Workflows;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class APIsKnowledgeBaseServiceSearchWorkflowStep(
    KnowledgeBaseServiceSearchWorkflowStep knowledgeBaseServiceSearchWorkflowExecutor) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string APIsDocumentationCollectionName = "apis";
    private const string WorkflowStepDisplayName = "APIs Knowledge Base Service Search";

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

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteAPIsKnowledgeBaseServiceSearchAsync(stateObject);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

