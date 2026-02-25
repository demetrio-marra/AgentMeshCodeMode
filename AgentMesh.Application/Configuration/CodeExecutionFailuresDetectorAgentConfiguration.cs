namespace AgentMesh.Application.Services
{
    public class CodeExecutionFailuresDetectorAgentConfiguration
    {
        public const string SectionName = "Agents:CodeExecutionFailuresDetector";
        public const string AgentName = "CodeExecutionFailuresDetector";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
