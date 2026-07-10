using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Models.RequirementsCollector;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Workflows.Steps;

public class RequirementsCollectorWorkflowStep(
    ILogger<CodeModeWorkflow> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    IRequirementsCollectorAgent requirementsCollectorAgent,
    CodeModeWorkflowConfiguration workflowConfiguration)
{
    private readonly ILogger<CodeModeWorkflow> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly IRequirementsCollectorAgent _requirementsCollectorAgent = requirementsCollectorAgent;
    private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;
    private const string QmdQueryTypesFileName = "QMDQueryTypes.md";

    public async Task ExecuteRequirementsCollectorAsync(CodeModeWorkflowState state)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Requirements Collector Agent...");

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Requirements Collector Agent", new Dictionary<string, string>
        {
            { "UserIntent", state.ClassifiedUserRequest.Intent },
            { "UserIntentCategory", state.ClassifiedUserRequest.IntentCategory.ToString() },
            { "EntitiesByDomain", state.ClassifiedUserRequest.EntitiesByDomain.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(e => $"[{kvp.Key}] {e}"))) : "(No entities)" },
            { "SupportingIntentInformation", state.ClassifiedUserRequest.SupportingIntentInformation.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.SupportingIntentInformation) : "(No supporting intent information)" },
            { "UserPreferences", state.ClassifiedUserRequest.UserPreferences.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.UserPreferences) : "(No user preferences)" },
            { "MissingMemories", state.ClassifiedUserRequest.MissingMemories.Any() ? WorkflowExecutorFormatting.ToBulletList(state.ClassifiedUserRequest.MissingMemories) : "(No missing memories)" },
            { "FastKnowledgeBaseResults", state.FastDomainsKnowledgeBaseQueryResults.Results.Any() ? WorkflowExecutorFormatting.ToBulletList(state.FastDomainsKnowledgeBaseQueryResults.Results.Select(r => $"[{r.File}] {r.Title}")) : "(No fast knowledge base results)" }
        });

        var output = await _requirementsCollectorAgent.ExecuteAsync(new RequirementsCollectorAgentInput
        {
            UserIntent = state.CanonicalizedIntent,
            UserIntentCategory = state.ClassifiedUserRequest.IntentCategory,
            EntitiesByDomain = state.ClassifiedUserRequest.EntitiesByDomain,
            SupportingIntentInformation = state.ClassifiedUserRequest.SupportingIntentInformation,
            UserPreferences = state.ClassifiedUserRequest.UserPreferences,
            MissingMemories = state.ClassifiedUserRequest.MissingMemories,
            FastKnowledgeBaseQueryResults = state.FastDomainsKnowledgeBaseQueryResults.Results,
            LanguageOfKnowledgeBase = _workflowConfiguration.LanguageOfKnowledgeBase,
            QmdQueryTypesReference = LoadQmdQueryTypesReference()
        });

        state.PastMemoriesQuery = output.MissingPastMemories;
        state.DomainsKnowledgeBaseQuery = output.MissingKnowledgeBaseSearchEntries;

        state.AddTokenUsage(RequirementsCollectorAgentConfiguration.AgentName, output.InputTokenCount, output.OutputTokenCount, stopwatch.Elapsed, "Requirements Collector Agent");

        var notifyDictionary = new Dictionary<string, string>();
        if (state.PastMemoriesQuery.Any())
        {
            notifyDictionary.Add("MissingPastMemoriesDetails", WorkflowExecutorFormatting.ToBulletList(state.PastMemoriesQuery));
        }
        if (state.DomainsKnowledgeBaseQuery.Any())
        {
            notifyDictionary.Add("MissingKnowledgeBaseEntriesDetails", WorkflowExecutorFormatting.ToBulletList(state.DomainsKnowledgeBaseQuery));
        }
        notifyDictionary.Add("ELAPSED_TIME", WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed));
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Requirements Collector Agent", notifyDictionary);
    }

    private string? LoadQmdQueryTypesReference()
    {
        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Prompts", QmdQueryTypesFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Prompts", QmdQueryTypesFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "AgentMeshCLI", "Prompts", QmdQueryTypesFileName)
        };

        foreach (var candidatePath in candidatePaths)
        {
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            return File.ReadAllText(candidatePath);
        }

        _logger.LogWarning("Unable to locate QMD query types prompt file '{FileName}' in expected paths.", QmdQueryTypesFileName);
        return null;
    }
}

