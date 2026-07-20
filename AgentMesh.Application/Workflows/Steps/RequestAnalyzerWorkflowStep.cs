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

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Request Analyzer Agent", new Dictionary<string, string>
        {
            { "ContextMessages", "<omitted for brevity>. Total: " + chatHistory.Count().ToString() },
            { "UserLastRequest", state.UserLastRequest }
        });

        var agentOutput = await _requestAnalyzerAgent.ExecuteAsync(new AgentMesh.Models.RequestAnalysis.RequestAnalyzerAgentInput
        {
            ContextMessages = [.. state.InitialContextMessages],
            UserLastRequest = state.UserLastRequest
        });

        state.NewStructuredUserRequest = agentOutput;

        state.AddTokenUsage(RequestAnalyzerAgent.AgentName, agentOutput.InputTokenCount, agentOutput.OutputTokenCount, stopwatch.Elapsed, "Request Analyzer Agent");

        var notifyDictionary = new Dictionary<string, string>
        {
            { "Intent", agentOutput.Intent },
            { "Conversation topic", agentOutput.ConversationTopic ?? "(No conversation topic extracted)" },
            { "Language of the user", agentOutput.LanguageOfTheUser },
            { "User requested actions", WorkflowExecutorFormatting.ToBulletList(agentOutput.UserRequestedActions) },
            { "User preferences", WorkflowExecutorFormatting.ToBulletList(agentOutput.UserPreferences) },
            { "User provided data", WorkflowExecutorFormatting.ToBulletList(agentOutput.UserProvidedData) },
            { "Missing values", WorkflowExecutorFormatting.ToBulletList(agentOutput.MissingValues) }
        };
       
        notifyDictionary.Add("ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed));
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Request Analyzer Agent", notifyDictionary);
    }
}

