namespace AgentMesh.Models.Workflows
{
    public record struct AgentResultRecord(IEnumerable<AgentOutputParameterRecord> OutputParameters, int InputTokens, int OutputTokens);
}
