namespace AgentMesh.Application.Services
{
    public class TranslatorAgentConfiguration
    {
        public const string SectionName = "Agents:Translator";
        public const string AgentName = "Translator";

        public string Provider { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
        public decimal CostPerMillionInputTokens { get; set; }
        public decimal CostPerMillionOutputTokens { get; set; }
    }
}
