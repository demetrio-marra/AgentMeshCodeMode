using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class DomainsKnowledgeBaseDocumentsExtractorWorkflowStep(
    KnowledgeBaseDocumentsExtractorWorkflowStep knowledgeBaseDocumentsExtractorWorkflowExecutor) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Domains Knowledge Base Documents Extractor";

    private readonly KnowledgeBaseDocumentsExtractorWorkflowStep _knowledgeBaseDocumentsExtractorWorkflowExecutor = knowledgeBaseDocumentsExtractorWorkflowExecutor;

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

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteDomainsKnowledgeBaseDocumentsExtractorAsync(stateObject);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

public partial class DomainsKnowledgeBaseDocumentsExtractorWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => false;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => null;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(EWParameterNames.KnowledgeBaseQueryResults, false)
    ];

    public override async Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default)
    {
        return await _knowledgeBaseDocumentsExtractorWorkflowExecutor.ExecuteAsync(inputParameters, cancellationToken);
    }
}

