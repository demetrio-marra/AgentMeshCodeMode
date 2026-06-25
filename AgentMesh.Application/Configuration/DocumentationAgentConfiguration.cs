namespace AgentMesh.Application.Configuration
{
    public class DocumentationAgentConfiguration
    {
        public const string SectionName = "Agents:Documentation";
        public const string AgentName = "Documentation";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
