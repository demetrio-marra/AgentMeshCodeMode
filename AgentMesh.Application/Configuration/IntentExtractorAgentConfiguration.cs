namespace AgentMesh.Application.Services
{
    public class IntentExtractorAgentConfiguration
    {
        public const string SectionName = "Agents:IntentExtractor";
        public const string AgentName = "IntentExtractor";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
