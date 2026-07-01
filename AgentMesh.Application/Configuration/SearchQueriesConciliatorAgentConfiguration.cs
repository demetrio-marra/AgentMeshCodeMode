namespace AgentMesh.Application.Configuration
{
    public class SearchQueriesConciliatorAgentConfiguration
    {
        public const string SectionName = "Agents:SearchQueriesConciliator";
        public const string AgentName = "SearchQueriesConciliator";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
