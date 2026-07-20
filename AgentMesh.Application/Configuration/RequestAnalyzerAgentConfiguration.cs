namespace AgentMesh.Application.Services
{
    public class RequestAnalyzerAgentConfiguration
    {
        public const string SectionName = "Agents:RequestAnalyzer";
        public const string AgentName = "RequestAnalyzer";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
