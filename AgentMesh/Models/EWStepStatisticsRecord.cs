namespace AgentMesh.Models
{
    public record struct EWStepStatisticsRecord(string StepName,
        DateTime StartedOnUtc,
        DateTime CompletedOnUtc,
        IEnumerable<EWDisplayParameterRecord> ParametersBefore,
        IEnumerable<EWDisplayParameterRecord> ParametersAfter,
        bool IsAgentic,
        string? AgentName,
        bool CountInputTokensAsContextTokens,
        bool CountOutputTokensAsContextTokens,
        int? InputTokens,
        int? OutputTokens)
    {
        public readonly TimeSpan Elapsed { get => CompletedOnUtc - StartedOnUtc; }
        public readonly int? TotalTokens { get => !IsAgentic ? null : (InputTokens ?? 0) + (OutputTokens ?? 0); }

        public readonly string HumanReadableElapsed
        {
            get
            {
                var elapsed = Elapsed;
                var elapsedParts = new List<string>();

                if (elapsed.Hours > 0)
                {
                    elapsedParts.Add($"{elapsed.Hours}h");
                }
                if (elapsed.Minutes > 0)
                {
                    elapsedParts.Add($"{elapsed.Minutes}m");
                }
                if (elapsed.Seconds > 0)
                {
                    elapsedParts.Add($"{elapsed.Seconds}s");
                }
                
                if (elapsedParts.Count == 0)
                {
                    return "<1s";
                }

                return string.Join(" ", elapsedParts);
            }
        }

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
