namespace AgentMesh.Application.Configuration
{
    public class FunctionalAnalystAgentConfiguration
    {
        public const string SectionName = "Agents:FunctionalAnalyst";
        public const string AgentName = "FunctionalAnalyst";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
