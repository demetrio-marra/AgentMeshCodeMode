namespace AgentMesh.Application.Services
{
    public class RouterAgentConfiguration
    {
        public const string SectionName = "Agents:Router";
        public const string AgentName = "Router";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
