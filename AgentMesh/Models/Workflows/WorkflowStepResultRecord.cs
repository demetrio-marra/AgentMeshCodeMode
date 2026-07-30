namespace AgentMesh.Models.Workflows
{
    public record struct WorkflowStepResultRecord(Dictionary<string, string?> OutputParameters, AgentTokenUsageEntry? AgentTokenUsageEntry);
}
