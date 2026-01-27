namespace AgentMesh.Application.Services
{
    public class CodeStaticAnalyzerConfiguration
    {
        public const string SectionName = "Agents:CodeStaticAnalyzer";
        public const string AgentName = "CodeStaticAnalyzer";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
