namespace AgentMesh.Models.Workflows
{
    public record struct WorkflowResultRecord(string ResponseForUser, int ContextSizeInTokens, IEnumerable<WorkflowStepStatisticsRecord> Steps);
}
