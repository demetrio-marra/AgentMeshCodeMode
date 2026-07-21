using AgentMesh.Application.Models;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class RequestAnalyzerWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IRequestAnalyzerAgent requestAnalyzerAgent)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IRequestAnalyzerAgent _requestAnalyzerAgent = requestAnalyzerAgent;

    public async Task ExecuteRequestAnalyzerAsync(CodeModeWorkflowState state, IEnumerable<ContextMessage> chatHistory)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Request Analyzer Agent...");

        var agentInput = new AgentMesh.Models.RequestAnalysis.RequestAnalyzerAgentInput
        {
            ContextMessages = [.. state.InitialContextMessages],
            UserLastRequest = state.UserLastRequest
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Request Analyzer Agent", agentInput.ToDictionary());

        var agentOutput = await _requestAnalyzerAgent.ExecuteAsync(agentInput);

        state.NewStructuredUserRequest = agentOutput;

        state.AddTokenUsage(RequestAnalyzerAgent.AgentName, agentOutput.InputTokenCount, agentOutput.OutputTokenCount, stopwatch.Elapsed, "Request Analyzer Agent");

        var notifyDictionary = agentOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Request Analyzer Agent", notifyDictionary);
    }
}
