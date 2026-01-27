namespace AgentMesh.Application.Services
{
    public class ContextAggregatorAgentConfiguration
    {
        public const string SectionName = "Agents:ContextAggregator";
        public const string AgentName = "ContextAggregator";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
