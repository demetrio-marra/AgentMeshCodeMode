namespace AgentMesh.Application.Services
{
    public class SearchQueriesGeneratorAgentConfiguration
    {
        public const string SectionName = "Agents:SearchQueriesGenerator";
        public const string AgentName = "SearchQueriesGenerator";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
