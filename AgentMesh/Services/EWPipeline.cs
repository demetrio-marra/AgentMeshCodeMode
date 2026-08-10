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

            var stepRuns = new List<PlannedStepsRun>();
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

            var fistAgenticStepStatistics = stepRuns.FirstOrDefault(s => s.Step is IEWAgenticStep agenticStep && agenticStep.IsInputTokensCountSource)?.Statistics;
            var lastAgenticStepStatistics = stepRuns.LastOrDefault(s => s.Step is IEWAgenticStep agenticStep && agenticStep.IsOutputTokensCountSource)?.Statistics;

            var inputTokens = fistAgenticStepStatistics?.InputTokens ?? 0;
            var outputTokens = lastAgenticStepStatistics?.OutputTokens ?? 0;
            var contextSizeInTokens = inputTokens + outputTokens;

            await _workflowProgressNotifier.NotifyWorkflowEnd();

            return new EWResultRecord(
                ResponseForUser: responseForUserParameter.GetDisplayValue(),
                ContextSizeInTokens: contextSizeInTokens,
                Steps: [.. stepRuns.Select(s => s.Statistics)]);
        }

        private async Task<PlannedStepsRun> RunStep(
            IEWStep step,
            CancellationToken cancellationToken)
        {
            var parametersBeforeSnapshot = CreateDisplaySnapshot(_ewParametersProvider.GetParameters());

            EWAgenticStepResultRecord? stepResultRecord = null;
            string? agentName = null;
            int? inputTokens = null;
            int? outputTokens = null;
            bool isAgentic = step is IEWAgenticStep;
            bool isLastAgenticStep = false;
            bool isFirstAgenticStep = false;
            var stepStartTime = DateTime.UtcNow;
            if (step is IEWAgenticStep agenticStep)
            {
                stepResultRecord = await agenticStep.ExecuteAsync(cancellationToken);
                agentName = agenticStep.AgentName;
                inputTokens = stepResultRecord?.InputTokens;
                outputTokens = stepResultRecord?.OutputTokens;
                isFirstAgenticStep = agenticStep.IsInputTokensCountSource;
                isLastAgenticStep = agenticStep.IsOutputTokensCountSource;
            }
            else if (step is IEWCodeStep codeStep)
            {
                await codeStep.ExecuteAsync(cancellationToken);
            }
            else
            {
                throw new NotImplementedException($"Step type '{step.GetType().Name}' is not supported.");
            }

            var stepEndTime = DateTime.UtcNow;

            var parametersAfterSnapshot = CreateDisplaySnapshot(_ewParametersProvider.GetParameters());

            var stepStatistics = new EWStepStatisticsRecord(
                StepName: step.Name,
                StartedOnUtc: stepStartTime,
                CompletedOnUtc: stepEndTime,
                IsFirstAgenticStep: isFirstAgenticStep,
                IsLastAgenticStep: isLastAgenticStep,
                ParametersBefore: parametersBeforeSnapshot,
                ParametersAfter: parametersAfterSnapshot,
                IsAgentic: isAgentic,
                AgentName: agentName,
                InputTokens: inputTokens,
                OutputTokens: outputTokens
            );

            return new PlannedStepsRun
            {
                Step = step,
                Result = stepResultRecord,
                Statistics = stepStatistics
            };
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

        private class PlannedStepsRun
        {
            public required IEWStep Step { get; set; }
            public EWAgenticStepResultRecord? Result { get; set; }
            public EWStepStatisticsRecord Statistics { get; set; }
        }
    }
}
