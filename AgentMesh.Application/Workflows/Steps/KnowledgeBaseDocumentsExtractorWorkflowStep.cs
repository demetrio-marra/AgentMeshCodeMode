using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class KnowledgeBaseDocumentsExtractorWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    KnowledgeBaseExecutor knowledgeBaseGetDocsExecutor) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Knowledge Base Documents Extractor";

    private readonly ILogger<CodeModeWorkflow> _logger = logger;
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

