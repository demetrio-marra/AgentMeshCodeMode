namespace AgentMesh.Models.Workflows
{
    public record struct EWResultRecord(string ResponseForUser,
        int ContextSizeInTokens, 
        IEnumerable<EWStepStatisticsRecord> Steps);
}
