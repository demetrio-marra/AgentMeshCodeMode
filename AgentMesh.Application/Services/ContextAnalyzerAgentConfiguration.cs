namespace AgentMesh.Application.Services
{
    public class ContextAnalyzerAgentConfiguration
    {
        public const string SectionName = "Agents:ContextAnalyzer";
        public const string AgentName = "ContextAnalyzer";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
