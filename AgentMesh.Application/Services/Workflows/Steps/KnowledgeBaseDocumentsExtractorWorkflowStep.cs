using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class KnowledgeBaseDocumentsExtractorWorkflowStep(
    ILogger<KnowledgeBaseDocumentsExtractorWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    KnowledgeBaseExecutor knowledgeBaseGetDocsExecutor) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Knowledge Base Documents Extractor";

    private readonly ILogger<KnowledgeBaseDocumentsExtractorWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly KnowledgeBaseExecutor _knowledgeBaseGetDocsExecutor = knowledgeBaseGetDocsExecutor;

    public async Task ExecuteKnowledgeBaseDocumentsExtractorAsync(
        CodeModeWorkflowState state,
        string logMessage,
        string stepName,
        string startNotificationKey,
        Func<CodeModeWorkflowState, IEnumerable<string>> getFilePaths,
        Func<string?, string> normalizeFilePath,
        StringComparer distinctComparer,
        Func<IEnumerable<KnowledgeBaseGetDocsOutputItem>, Dictionary<string, KnowledgeBaseDocumentContent>> buildDocumentsByFile,
        Action<CodeModeWorkflowState, IReadOnlyCollection<KnowledgeBaseDocumentContent>> setDocuments)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(logMessage);

        var filesToExtract = getFilePaths(state)
            .Select(normalizeFilePath)
            .Where(file => !string.IsNullOrWhiteSpace(file))
            .Distinct(distinctComparer)
            .ToList();

        await _workflowProgressNotifier.NotifyWorkflowStepStart(stepName, new Dictionary<string, string>
        {
            { startNotificationKey, WorkflowExecutorFormatting.ToBulletList(filesToExtract) }
        });

        var fetchedFilesContent = await _knowledgeBaseGetDocsExecutor.GetDocsAsync(new KnowledgeBaseGetDocsInput
        {
            FilePaths = filesToExtract
        }, CancellationToken.None);

        var documentsByFile = buildDocumentsByFile(fetchedFilesContent.Results);
        var documents = documentsByFile.Values.ToList();
        setDocuments(state, documents);

        state.AddStepUsage(stepName, stopwatch.Elapsed, false);

        var notifyDictionary = new Dictionary<string, string>
        {
            { "Total files extracted", WorkflowExecutorFormatting.ToBulletList(documents.Select(doc => doc.File)) },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd(stepName, notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        await ExecuteKnowledgeBaseDocumentsExtractorAsync(
            stateObject,
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

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

public partial class KnowledgeBaseDocumentsExtractorWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => false;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => null;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(CodeModeWorkflowParametersFactory.KnowledgeBaseQueryResultsParameterName, false)
    ];

    public override async Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default)
    {
        var queryResultsValue = inputParameters.FirstOrDefault(p => p.Name == CodeModeWorkflowParametersFactory.KnowledgeBaseQueryResultsParameterName).RawValue ?? string.Empty;
        var filePaths = queryResultsValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var fetchedFilesContent = await _knowledgeBaseGetDocsExecutor.GetDocsAsync(new KnowledgeBaseGetDocsInput
        {
            FilePaths = filePaths
        }, cancellationToken);

        var documents = fetchedFilesContent.Results
            .Where(doc => !string.IsNullOrWhiteSpace(doc.File))
            .GroupBy(doc => doc.File!)
            .Select(g => new KnowledgeBaseDocumentContent { File = g.Key, Content = g.First().Content })
            .ToList();

        return new WorkflowStepResultRecord
        {
            OutputParameters = new Dictionary<string, string?>
            {
                { CodeModeWorkflowParametersFactory.DomainsKnowledgeBaseDocumentsContentParameterName, string.Join(", ", documents.Select(d => d.File)) }
            }
        };
    }
}

