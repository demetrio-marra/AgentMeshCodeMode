namespace AgentMesh.Application.Configuration
{
    public class DomainExpertAgentConfiguration
    {
        public const string SectionName = "Agents:DomainExpert";
        public const string AgentName = "DomainExpert";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
