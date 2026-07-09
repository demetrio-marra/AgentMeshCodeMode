using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Workflows;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Coder;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class CoderWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    ICoderAgent coderAgent)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly ICoderAgent _coderAgent = coderAgent;

    public async Task ExecuteCoderAsync(CodeModeWorkflowState state)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Coder Agent...");
        var businessRequirements = state.BusinessRequirements ?? "(No business requirements)";
        var technicalSpecification = state.TechnicalSpecification ?? "(No technical specification)";

        var filteredDocuments = state.SelectedAPIsFileLocations.Any()
            ? state.KnowledgeBaseAPIDocumentsContent
                .Where(doc => state.SelectedAPIsFileLocations.Contains(doc.File, StringComparer.OrdinalIgnoreCase))
                .ToList()
            : [];

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Coder Agent", new Dictionary<string, string>
        {
            { "BusinessRequirements", businessRequirements },
            { "TechnicalSpecification", technicalSpecification },
            { "KnowledgeBaseAPIDocuments", filteredDocuments.Any() ? WorkflowExecutorFormatting.ToBulletList(filteredDocuments.Select(s => s.File)) : "(No documents)" }
        });

        var coderAgentOutput = await _coderAgent.ExecuteAsync(new CoderAgentInput
        {
            BusinessRequirements = businessRequirements,
            TechnicalSpecification = technicalSpecification,
            KnowledgeBaseAPIDocumentsContent = filteredDocuments.Select(doc => new KnowledgeBaseGetDocsOutputItem
            {
                File = doc.File,
                Content = doc.Content
            })
        });
        state.GeneratedCode = coderAgentOutput.CodeToRun;
        state.AddTokenUsage(CoderAgentConfiguration.AgentName, coderAgentOutput.InputTokenCount, coderAgentOutput.OutputTokenCount, stopwatch.Elapsed, "Coder Agent");
        var notifyDictionary = new Dictionary<string, string>
        {
            { "CodeToRun", state.GeneratedCode },
            { "ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed) }
        };
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Coder Agent", notifyDictionary);
    }
}

