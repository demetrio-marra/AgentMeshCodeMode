using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Models.Coder;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AgentMesh.Services;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows;

namespace AgentMesh.Application.Services.Workflows.Steps;

public partial class CoderWorkflowStep(
    ILogger<CoderWorkflowStep> logger,
    IWorkflowProgressNotifier workflowProgressNotifier,
    CoderAgent coderAgent,
    IAgentSelector agentSelector) : IWorkflowStep<CodeModeWorkflowState>
{
    private const string WorkflowStepDisplayName = "Coder";

    private readonly ILogger<CoderWorkflowStep> _logger = logger;
    private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
    private readonly CoderAgent _coderAgent = coderAgent;
    private readonly IAgentSelector _agentSelector = agentSelector;

    public async Task ExecuteCoderAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging Coder Agent...");

        var docsToPass = state.KnowledgeBaseAPIDocumentsContent.Select(doc => new KnowledgeBaseGetDocsOutputItem
        {
            File = doc.File,
            Content = doc.Content
        });

        var agentInput = new CoderAgentInput
        {
            BusinessRequirements = state.BusinessRequirements ?? "(No business requirements)",
            TechnicalSpecification = state.TechnicalSpecification ?? "(No technical specification)",
            SelectedAPIsFileLocations = state.SelectedAPIsFileLocations,
            KnowledgeBaseAPIDocumentsContent = docsToPass
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("Coder Agent", agentInput.ToDictionary());

        var coderAgentOutput = await _coderAgent.ExecuteAsync(agentInput, cancellationToken);
        state.GeneratedCode = coderAgentOutput.CodeToRun;
        state.AddTokenUsage(CoderAgentConfiguration.AgentName, coderAgentOutput.InputTokenCount, coderAgentOutput.OutputTokenCount, stopwatch.Elapsed, "Coder Agent");

        var notifyDictionary = coderAgentOutput.ToDictionary();
        notifyDictionary["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
        await _workflowProgressNotifier.NotifyWorkflowStepEnd("Coder Agent", notifyDictionary);
    }

    public async Task<WorkflowStepUsageEntry> ExecuteAsync(CodeModeWorkflowState stateObject, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await ExecuteCoderAsync(stateObject, cancellationToken);

        return new WorkflowStepUsageEntry
        {
            StepName = WorkflowStepDisplayName,
            Elapsed = stopwatch.Elapsed,
            IsAgentic = false
        };
    }
}

public partial class CoderWorkflowStep : EasyWorkflowStepBase
{
    public override string Name => WorkflowStepDisplayName;

    public override bool IsAgentic => true;

    public override bool IsInputStep => false;

    public override bool IsOutputStep => false;

    public override string? AgentName => CoderAgentConfiguration.AgentName;

    public override IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames => [
        new(CodeModeWorkflowParametersFactory.BusinessRequirementsParameterName, false),
        new(CodeModeWorkflowParametersFactory.TechnicalSpecificationParameterName, false),
        new(CodeModeWorkflowParametersFactory.SelectedAPIsFileLocationsParameterName, false),
        new(CodeModeWorkflowParametersFactory.KnowledgeBaseAPIDocumentsContentParameterName, true)
    ];

    public override async Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default)
    {
        var agentInput = ToAgentInputParameters(inputParameters);

        var agent = _agentSelector.GetAgent(AgentName!);
        var agentOutput = await agent.ExecuteAsync(agentInput, cancellationToken);

        return new WorkflowStepResultRecord
        {
            OutputParameters = agentOutput.OutputParameters.ToDictionary(p => p.Name, p => p.Value),
            AgentTokenUsageEntry = new AgentTokenUsageEntry
            {
                AgentName = CoderAgentConfiguration.AgentName,
                InputTokens = agentOutput.InputTokens,
                OutputTokens = agentOutput.OutputTokens
            }
        };
    }
}

