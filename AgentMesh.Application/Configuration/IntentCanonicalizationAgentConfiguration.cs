namespace AgentMesh.Application.Configuration
{
    public class IntentCanonicalizationAgentConfiguration
    {
        public const string SectionName = "Agents:IntentCanonicalization";
        public const string AgentName = "IntentCanonicalization";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
