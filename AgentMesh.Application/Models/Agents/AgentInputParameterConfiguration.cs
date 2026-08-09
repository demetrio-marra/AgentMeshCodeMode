namespace AgentMesh.Application.Models.Agents
{
    public readonly record struct AgentInputParameterConfiguration
    {
        public string ParameterName { get; init; }
        public IEnumerable<string> ParameterTags { get; init; }
    }
}
