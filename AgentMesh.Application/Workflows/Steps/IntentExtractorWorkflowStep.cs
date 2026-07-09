using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Models;
using AgentMesh.Models.IntentExtractor;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class IntentExtractorWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IIntentExtractorAgent intentExtractorAgent,
    CodeModeWorkflowConfiguration workflowConfiguration)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IIntentExtractorAgent _intentExtractorAgent = intentExtractorAgent;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;

    public async Task ExecuteIntentExtractorAsync(CodeModeWorkflowState state, IEnumerable<ContextMessage> chatHistory)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Intent Extractor Agent...");

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Intent Extractor Agent", new Dictionary<string, string>
        {
            { "ContextMessages", "<omitted for brevity>. Total: " + chatHistory.Count().ToString() },
            { "UserLastRequest", state.UserLastRequest },
            { "LanguageOfKnowledgeBase", _workflowConfiguration.LanguageOfKnowledgeBase }
        });

        var intentExtractorOutput = await _intentExtractorAgent.ExecuteAsync(new IntentExtractorAgentInput
        {
            ContextMessages = [.. state.InitialContextMessages],
            UserLastRequest = state.UserLastRequest,
            ApplicationDomainList = _workflowConfiguration.ApplicationDomainList,
            LanguageOfKnowledgeBase = _workflowConfiguration.LanguageOfKnowledgeBase
        });

        state.ClassifiedUserRequest = new StructuredUserRequest
        {
            OriginalUserRequest = intentExtractorOutput.OriginalUserRequest,
            Intent = intentExtractorOutput.UserIntent,
            IntentCategory = intentExtractorOutput.UserIntentCategory,
            CanonicalizedIntentCategory = intentExtractorOutput.UserIntentCategory,
            LanguageOfTheUser = intentExtractorOutput.LanguageOfTheUser,
            EntitiesByDomain = intentExtractorOutput.EntitiesByDomain,
            SupportingIntentInformation = intentExtractorOutput.SupportingIntentInformation,
            UserPreferences = intentExtractorOutput.UserPreferences,
            MissingMemories = intentExtractorOutput.MissingMemories
        };
        state.CanonicalizedIntent = state.ClassifiedUserRequest.Intent ?? string.Empty;

        state.AddTokenUsage(IntentExtractorAgentConfiguration.AgentName, intentExtractorOutput.InputTokenCount, intentExtractorOutput.OutputTokenCount, stopwatch.Elapsed, "Intent Extractor Agent");

        var notifyDictionary = new Dictionary<string, string>
        {
            { "OriginalUserRequest", state.ClassifiedUserRequest.OriginalUserRequest },
            { "ExtractedIntent", state.ClassifiedUserRequest.Intent ?? "(No intent extracted)" }
        };
        if (state.ClassifiedUserRequest.LanguageOfTheUser != null)
        {
            notifyDictionary.Add("LanguageOfTheUser", state.ClassifiedUserRequest.LanguageOfTheUser);
        }
        notifyDictionary.Add("UserIntentCategory", state.ClassifiedUserRequest.IntentCategory.ToString());
        if (state.ClassifiedUserRequest.SupportingIntentInformation.Any())
        {
            notifyDictionary.Add("SupportingIntentInformation", WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation));
        }
        if (state.ClassifiedUserRequest.EntitiesByDomain.Any())
        {
            notifyDictionary.Add("EntitiesByDomain", WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp =>
                kvp.Value.Select(entity => $"[{kvp.Key}] {entity}"))));
        }
        if (state.ClassifiedUserRequest.UserPreferences.Any())
        {
            notifyDictionary.Add("UserPreferences", WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.UserPreferences));
        }
        if (state.ClassifiedUserRequest.MissingMemories.Any())
        {
            notifyDictionary.Add("MissingMemories", WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.MissingMemories));
        }
        notifyDictionary.Add("ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed));
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Intent Extractor Agent", notifyDictionary);
    }
}

