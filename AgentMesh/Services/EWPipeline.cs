using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public class EWPipeline
    {
        private readonly EWParametersProvider _ewParametersProvider;
        private readonly IEWStepSelector _ewStepSelector;
        private readonly IWorkflowProgressNotifier _workflowProgressNotifier;

        public EWPipeline(
            EWParametersProvider ewParametersProvider,
            IEWStepSelector ewStepSelector,
            IWorkflowProgressNotifier workflowProgressNotifier)
        {
            _ewParametersProvider = ewParametersProvider;
            _ewStepSelector = ewStepSelector;
            _workflowProgressNotifier = workflowProgressNotifier;
        }

        public async Task<EWResultRecord> ExecuteAsync(
            string userInput,
            IEnumerable<ContextMessage> chatHistory,
            CancellationToken cancellationToken = default)
        {
            await _workflowProgressNotifier.NotifyWorkflowStart();

            var userInputParameter = _ewParametersProvider.GetUserCurrentRequestParameter()
                ?? throw new InvalidOperationException("User input parameter is not defined.");
            var conversationHistoryParameter = _ewParametersProvider.GetConversationHistoryParameter()
                ?? throw new InvalidOperationException("Conversation history parameter is not defined.");

            SetParameterValue(userInputParameter, userInput);
            SetParameterValue(conversationHistoryParameter, chatHistory);

            var stepRuns = new List<(IEWStep Step, EWStepResultRecord Result, EWStepStatisticsRecord Statistics)>();
            var nextSteps = _ewStepSelector.NextStepsToRun();

            while (nextSteps.Any())
            {
                var stepTasks = nextSteps.Select(step => RunStep(step, cancellationToken)).ToList();

                foreach (var step in nextSteps)
                {
                    await _workflowProgressNotifier.NotifyWorkflowStepStarted(step.Name);
                }

                var currentStepRuns = await Task.WhenAll(stepTasks);

                stepRuns.AddRange(currentStepRuns);

                foreach (var cs in currentStepRuns)
                {
                    await _workflowProgressNotifier.NotifyWorkflowStepCompleted(cs.Step.Name, cs.Statistics);
                }

                nextSteps = _ewStepSelector.NextStepsToRun();
            }

            var responseForUserParameter = _ewParametersProvider.GetResponseForUserParameter()
                ?? throw new InvalidOperationException("Response for user parameter is not defined.");

            var inputTokens = stepRuns.Single(s => s.Step.IsPipelineFirst).Result.InputTokens;
            var outputTokens = stepRuns.Single(s => s.Step.IsPipelineLast).Result.OutputTokens;
            var contextSizeInTokens = inputTokens + outputTokens;

            await _workflowProgressNotifier.NotifyWorkflowEnd();

            return new EWResultRecord(
                ResponseForUser: responseForUserParameter.GetDisplayValue(),
                ContextSizeInTokens: contextSizeInTokens ?? 0,
                Steps: [.. stepRuns.Select(s => s.Statistics)]);
        }

        private async Task<(IEWStep Step, EWStepResultRecord Result, EWStepStatisticsRecord Statistics)> RunStep(
            IEWStep step,
            CancellationToken cancellationToken)
        {
            var parametersBeforeSnapshot = CreateDisplaySnapshot(_ewParametersProvider.GetParameters());

            var stepStartTime = DateTime.UtcNow;
            var stepResult = await step.ExecuteAsync(cancellationToken);
            var stepEndTime = DateTime.UtcNow;

            var parametersAfterSnapshot = CreateDisplaySnapshot(_ewParametersProvider.GetParameters());

            var stepStatistics = new EWStepStatisticsRecord(
                StepName: step.Name,
                StartedOnUtc: stepStartTime,
                CompletedOnUtc: stepEndTime,
                IsInputStep: step.IsPipelineFirst,
                IsOutputStep: step.IsPipelineLast,
                ParametersBefore: parametersBeforeSnapshot,
                ParametersAfter: parametersAfterSnapshot,
                IsAgentic: step.IsAgentic,
                AgentName: step.AgentName,
                InputTokens: stepResult.InputTokens,
                OutputTokens: stepResult.OutputTokens
            );

            return (step, stepResult, stepStatistics);
        }

        private static void SetParameterValue<T>(IEWParameter parameter, T value)
        {
            if (parameter is EWParameter<T> typedParameter)
            {
                typedParameter.ParameterValue = value;
                return;
            }

            throw new InvalidOperationException($"Parameter '{parameter.Name}' is not of expected type '{typeof(T).Name}'.");
        }

        private static List<EWDisplayParameterRecord> CreateDisplaySnapshot(IEnumerable<IEWParameter> parameters)
        {
            return [.. parameters.Select(parameter =>
                {
                    var displayValue = parameter.GetDisplayValue();
                    return new EWDisplayParameterRecord(parameter.Name, displayValue);
                })];
        }
    }
}
