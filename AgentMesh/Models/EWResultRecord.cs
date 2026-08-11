namespace AgentMesh.Models
{
    public record struct EWResultRecord(string ResponseForUser,
        int ContextSizeInTokens, 
        IEnumerable<EWStepStatisticsRecord> Steps);
}
