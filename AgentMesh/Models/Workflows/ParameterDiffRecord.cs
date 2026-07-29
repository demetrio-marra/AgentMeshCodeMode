namespace AgentMesh.Models.Workflows
{
    public record struct ParameterDiffRecord(string Name, 
        string? OldRawValue, string? NewRawValue,
        string? OldValueForLLM, string? NewValueForLLM, 
        string OldDisplayValue, string NewDisplayValue);
}
