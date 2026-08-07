namespace AgentMesh.Models.Workflows
{
    public record struct EWStepStatisticsRecord(string StepName,
        DateTime StartedOnUtc,
        DateTime CompletedOnUtc,
        bool IsInputStep,
        bool IsOutputStep,
        IEnumerable<EWDisplayParameterRecord> ParametersBefore,
        IEnumerable<EWDisplayParameterRecord> StepInputParameters,
        IEnumerable<EWDisplayParameterRecord> ParametersAfter,
        bool IsAgentic,
        string? AgentName,
        int? InputTokens,
        int? OutputTokens)
    {
        public readonly TimeSpan Elapsed { get => CompletedOnUtc - StartedOnUtc; }
        public readonly int? TotalTokens { get => !IsAgentic ? null : (InputTokens ?? 0) + (OutputTokens ?? 0); }

        public readonly IEnumerable<EWDisplayDiffParameterRecord> ParametersDiff
        {
            get
            {
                var beforeDict = ParametersBefore.ToDictionary(p => p.Name);
                var afterDict = ParametersAfter.ToDictionary(p => p.Name);
                var allKeys = new HashSet<string>(beforeDict.Keys.Concat(afterDict.Keys));
                foreach (var key in allKeys)
                {
                    beforeDict.TryGetValue(key, out var beforeParam);
                    afterDict.TryGetValue(key, out var afterParam);
                    if ((beforeParam.Value ?? string.Empty) != (afterParam.Value ?? string.Empty))
                    {
                        yield return new EWDisplayDiffParameterRecord(
                            Name: key,
                            OldValue: beforeParam.Value,
                            NewValue: afterParam.Value
                        );
                    }
                }
            }

        }
    }
}
