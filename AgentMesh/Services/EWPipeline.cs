using AgentMesh.Models;

namespace AgentMesh.Services
{
    public abstract class EWPipeline(IWorkflowProgressNotifier workflowProgressNotifier,
        IParameterStore parameterStore,
        IEnumerable<IEWParameterConfiguration> parameterConfigurations) : IEWPipeline
    {
        public void SetInitialParameters(IDictionary<Type, object?> initialParameters)
        {
            parameterStore.SetInitialValues(initialParameters);
        }

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
                    var stepInputParameters = parameterStore.CreateSnapshot(step.InputParameterTypes);
                    var inputParametersValueDictionary = stepInputParameters.Values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
                    var inputParametersDisplayRecords = GetDisplayParameterRecords(inputParametersValueDictionary, parameterConfigurations);
                    await workflowProgressNotifier.NotifyWorkflowStepStarted(step.Name, inputParametersDisplayRecords);
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
            var stepInputParameters = parameterStore.CreateSnapshot(step.InputParameterTypes);
            var inputParametersValueDictionary = stepInputParameters.Values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);

            // this is needed only for concurrency checks
            var stepOutputParameters = parameterStore.CreateSnapshot(step.OutputParameterTypes);
            var outputParametersVersionsDictionary = stepOutputParameters.Values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Version);

            var stepStartTime = DateTime.UtcNow;

            var stepResultRecord = await step.ExecuteAsync(inputParametersValueDictionary, cancellationToken);
            var stepEndTime = DateTime.UtcNow;

            var stepStatistics = new EWStepStatisticsRecord(
              StepName: step.Name,
              StartedOnUtc: stepStartTime,
              CompletedOnUtc: stepEndTime,
              InputParameters: GetDisplayParameterRecords(inputParametersValueDictionary, parameterConfigurations),
              ParametersBefore: [],
              ParametersAfter: []
           );

            if (stepResultRecord.OutputMutations.Any())
            {
                var parametersToCommit = stepResultRecord.OutputMutations.Select(m => new ParameterMutation(
                    ParameterVersion: outputParametersVersionsDictionary[m.Key],
                    ParameterType: m.Key,
                    NewValue: m.Value
                )).ToList();

                var parameterStoreCommitResult = parameterStore.TryCommit(step.Name,
                    parametersToCommit);

                stepStatistics.ParametersBefore = GetDisplayParameterRecords(parameterStoreCommitResult.ToDictionary(p => p.ParameterType, p => p.OldValue).AsReadOnly(), parameterConfigurations);
                stepStatistics.ParametersAfter = GetDisplayParameterRecords(parameterStoreCommitResult.ToDictionary(p => p.ParameterType, p => p.NewValue).AsReadOnly(), parameterConfigurations);
            }

            if (stepResultRecord is EWAgenticStepExecutionResult agenticStepResultRecord
                && step is IEWAgenticStep agenticStep)
            {
                stepStatistics.IsAgentic = true;
                stepStatistics.AgentName = agenticStep.AgentName;
                stepStatistics.InputTokens = agenticStepResultRecord.InputTokens;
                stepStatistics.OutputTokens = agenticStepResultRecord.OutputTokens;
                stepStatistics.CountInputTokensAsContextTokens = agenticStep.CountInputTokensAsContextTokens;
                stepStatistics.CountOutputTokensAsContextTokens = agenticStep.CountOutputTokensAsContextTokens;
            }

            return new PlannedStepsRun
            {
                Step = step,
                Result = stepResultRecord,
                Statistics = stepStatistics
            };
        }

        protected object? GetParameterRawValue(Type parameterType)
        {
            var snapshot = parameterStore.CreateSnapshot(new[] { parameterType });
            if (snapshot.Values.TryGetValue(parameterType, out var parameterValue))
            {
                return parameterValue.Value;
            }
            return null;
        }

        private class PlannedStepsRun
        {
            public required IEWStep Step { get; set; }
            public EWStepExecutionResult? Result { get; set; }
            public EWStepStatisticsRecord Statistics { get; set; }
        }

        private static IEnumerable<EWDisplayParameterRecord> GetDisplayParameterRecords(IReadOnlyDictionary<Type, object?> values,
            IEnumerable<IEWParameterConfiguration> parameterConfigurations)
        {
            var displayRecords = new List<EWDisplayParameterRecord>();
            foreach (var kvp in values)
            {
                var parameterType = kvp.Key;
                var value = kvp.Value;
                var config = parameterConfigurations.FirstOrDefault(c => c.GetType() == parameterType);
                if (config == null)
                {
                    throw new InvalidOperationException($"No configuration found for parameter type '{parameterType.Name}'.");
                }
                var displayValueSerializer = config.DisplayValueSerializer;
                var displayValue = displayValueSerializer.Serialize(value);
                displayRecords.Add(new EWDisplayParameterRecord(
                    Name: config.Name,
                    Value: displayValue
                ));
            }
            return displayRecords;
        }
    }
}
