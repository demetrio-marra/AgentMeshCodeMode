namespace AgentMesh.Models.Workflows
{
    public record struct ParameterRecord(string Name, string? RawValue, string ValueForLLM, string DisplayValue);
}
