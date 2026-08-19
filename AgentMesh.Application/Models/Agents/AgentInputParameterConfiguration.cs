namespace AgentMesh.Application.Models.Agents
{
    public readonly record struct AgentInputParameterConfiguration
    {
        public Type ParameterType { get; init; }
        public IEnumerable<string> ParameterTags { get; init; }
    }
}
