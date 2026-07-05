namespace AgentMesh.Application.Services
{
    public class RequirementsCollectorAgentConfiguration
    {
        public const string SectionName = "Agents:RequirementsCollector";
        public const string AgentName = "RequirementsCollector";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
