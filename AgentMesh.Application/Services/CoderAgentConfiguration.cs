namespace AgentMesh.Application.Services
{
    public class CoderAgentConfiguration
    {
        public const string SectionName = "Agents:Coder";
        public const string AgentName = "Coder";

        public string Provider { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
        public string ApiReference { get; set; } = string.Empty;
        public string? ApiReferenceFile { get; set; }
        public decimal CostPerMillionInputTokens { get; set; }
        public decimal CostPerMillionOutputTokens { get; set; }
    }
}
