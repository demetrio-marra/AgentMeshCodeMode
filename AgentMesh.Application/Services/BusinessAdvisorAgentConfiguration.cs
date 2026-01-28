namespace AgentMesh.Application.Services
{
    public class BusinessAdvisorAgentConfiguration
    {
        public const string SectionName = "Agents:BusinessAdvisor";
        public const string AgentName = "BusinessAdvisor";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
        public string ApiDocumentation { get; set; } = string.Empty;
        public string? ApiDocumentationFile { get; set; }
    }
}
