namespace AgentMesh.Application.Services
{
    public class PersonalAssistantAgentConfiguration
    {
        public const string SectionName = "Agents:PersonalAssistant";
        public const string AgentName = "PersonalAssistant";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
