using AgentMesh.Services;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.KnowledgeBase;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Executors;

public class KnowledgeBaseDocumentsExtractorWorkflowExecutor(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IKnowledgeBaseGetDocsExecutor knowledgeBaseGetDocsExecutor)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IKnowledgeBaseGetDocsExecutor _knowledgeBaseGetDocsExecutor = knowledgeBaseGetDocsExecutor;

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

        var fetchedFilesContent = await _knowledgeBaseGetDocsExecutor.ExecuteAsync(new KnowledgeBaseGetDocsInput
        {
            FilePaths = filesToExtract
        });

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
}

