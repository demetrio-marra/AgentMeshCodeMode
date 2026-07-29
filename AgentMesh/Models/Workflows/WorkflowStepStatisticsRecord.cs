namespace AgentMesh.Models.Workflows
{
    public record struct WorkflowStepStatisticsRecord(string StepName,
        DateTime StartedOnUtc,
        DateTime CompletedOnUtc,
        bool IsAgentic,
        bool IsInputStep,
        bool IsOutputStep,
        IEnumerable<ParameterRecord> ParametersBefore,
        IEnumerable<ParameterRecord> ParametersAfter,
        AgentTokenUsageEntry? AgentTokenUsageEntry)
    {
        public readonly TimeSpan Elapsed { get => CompletedOnUtc - StartedOnUtc; }
        public readonly IEnumerable<ParameterDiffRecord> ParametersDiff { get
            {  
                var beforeDict = ParametersBefore.ToDictionary(p => p.Name);
                var afterDict = ParametersAfter.ToDictionary(p => p.Name);
                var allKeys = new HashSet<string>(beforeDict.Keys.Concat(afterDict.Keys));
                foreach (var key in allKeys)
                {
                    beforeDict.TryGetValue(key, out var beforeParam);
                    afterDict.TryGetValue(key, out var afterParam);
                    if ((beforeParam.RawValue ?? string.Empty) != (afterParam.RawValue ?? string.Empty))
                    {
                        yield return new ParameterDiffRecord(
                            Name: key,
                            OldRawValue: beforeParam.RawValue,
                            NewRawValue: afterParam.RawValue,
                            OldValueForLLM: beforeParam.ValueForLLM,
                            NewValueForLLM: afterParam.ValueForLLM,
                            OldDisplayValue: beforeParam.DisplayValue,
                            NewDisplayValue: afterParam.DisplayValue
                        );
                    }
                }
            }
        }
    }
}
