namespace AgentMesh.Models.Workflows
{
    public record struct EWStepResultRecord(string StepName,
        IEnumerable<EWParameterRecord> ChangedParameters,
        string? AgentName,
        int? InputTokens,
        int? OutputTokens);
}
