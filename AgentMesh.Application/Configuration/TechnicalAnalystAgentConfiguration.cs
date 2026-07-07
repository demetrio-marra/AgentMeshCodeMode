namespace AgentMesh.Application.Configuration
{
    // intentionally left blank
    public class TechnicalAnalystAgentConfiguration
    {
        public const string SectionName = "Agents:TechnicalAnalyst";
        public const string AgentName = "TechnicalAnalyst";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
