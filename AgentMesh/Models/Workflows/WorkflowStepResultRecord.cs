namespace AgentMesh.Models.Workflows
{
    public record struct WorkflowStepResultRecord(IEnumerable<ParameterRecord> OutputParameters, AgentTokenUsageEntry? AgentTokenUsageEntry);
}
