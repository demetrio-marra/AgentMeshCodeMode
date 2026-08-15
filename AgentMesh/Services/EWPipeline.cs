using AgentMesh.Models;

namespace AgentMesh.Services
{
    public abstract class EWPipeline(IWorkflowProgressNotifier workflowProgressNotifier,
        IEnumerable<IEWParameter> parameters) : IEWPipeline
    {
        public async Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await workflowProgressNotifier.NotifyWorkflowStart();

            var stepRuns = new List<PlannedStepsRun>();
            var nextSteps = GetNextStepsToRun();

            while (nextSteps.Any())
            {
                var stepTasks = nextSteps.Select(step => RunStep(step, cancellationToken)).ToList();

                foreach (var step in nextSteps)
                {
                    await workflowProgressNotifier.NotifyWorkflowStepStarted(step.Name);
                }

                var currentStepRuns = await Task.WhenAll(stepTasks);

                stepRuns.AddRange(currentStepRuns);

                foreach (var cs in currentStepRuns)
                {
                    await workflowProgressNotifier.NotifyWorkflowStepCompleted(cs.Step.Name, cs.Statistics);
                }

                nextSteps = GetNextStepsToRun();
            }

            await workflowProgressNotifier.NotifyWorkflowEnd();

            return stepRuns.Select(s => s.Statistics).ToList();
        }


        /// <summary>
        /// Returns the next steps to run in the workflow based on the provided parameters.
        /// If no more steps are available, it returns an empty collection.
        /// </summary>
        /// <returns>The steps to run</returns>
        /// <remarks>If more than one step are returned, they could be run in parallel</remarks>
        protected abstract IEnumerable<IEWStep> GetNextStepsToRun();

        private async Task<PlannedStepsRun> RunStep(
            IEWStep step,
            CancellationToken cancellationToken)
        {
            var parametersBeforeSnapshot = CreateDisplaySnapshot(parameters);

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

            var parametersAfterSnapshot = CreateDisplaySnapshot(parameters);

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
