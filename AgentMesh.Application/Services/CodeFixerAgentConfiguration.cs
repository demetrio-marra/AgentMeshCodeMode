namespace AgentMesh.Application.Services
{
    public class CodeFixerAgentConfiguration
    {
        public const string SectionName = "Agents:CodeFixer";
        public const string AgentName = "CodeFixer";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
