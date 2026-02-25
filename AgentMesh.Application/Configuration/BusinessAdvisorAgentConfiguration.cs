namespace AgentMesh.Application.Configuration
{
    public class BusinessAdvisorAgentConfiguration
    {
        public const string SectionName = "Agents:BusinessAdvisor";
        public const string AgentName = "BusinessAdvisor";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
