using AgentMesh.Application.Models;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Application.Models.RequestAnalysis;

namespace AgentMesh.Application.Workflows.Steps;

public class RequestAnalyzerWorkflowStep(
    ILogger<RequestAnalyzerWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    RequestAnalyzerAgent requestAnalyzerAgent) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Request Analyzer";

    private readonly ILogger<RequestAnalyzerWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly RequestAnalyzerAgent _requestAnalyzerAgent = requestAnalyzerAgent;

    public async Task ExecuteRequestAnalyzerAsync(CodeModeWorkflowState state, IEnumerable<ContextMessage> chatHistory)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Request Analyzer Agent...");

        var agentInput = new RequestAnalyzerAgentInput
        {
            ContextMessages = [.. state.InitialContextMessages],
            UserLastRequest = state.UserLastRequest
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Request Analyzer Agent", agentInput.ToDictionary());

        var agentOutput = await _requestAnalyzerAgent.ExecuteAsync(agentInput);

        state.UserRequest = agentOutput;

        state.AddTokenUsage(RequestAnalyzerAgent.AgentName, agentOutput.InputTokenCount, agentOutput.OutputTokenCount, stopwatch.Elapsed, "Request Analyzer Agent");

        var notifyDictionary = agentOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Request Analyzer Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteRequestAnalyzerAsync(stateObject, stateObject.InitialContextMessages);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}
