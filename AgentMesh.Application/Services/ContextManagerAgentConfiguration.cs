namespace AgentMesh.Application.Services
{
    public class ContextManagerAgentConfiguration
    {
        public const string SectionName = "Agents:ContextManager";
        public const string AgentName = "ContextManager";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
