namespace AgentMesh.Application.Configuration
{
    public class RequestCanonicalizationAgentConfiguration
    {
        public const string SectionName = "Agents:RequestCanonicalization";
        public const string AgentName = "RequestCanonicalization";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
