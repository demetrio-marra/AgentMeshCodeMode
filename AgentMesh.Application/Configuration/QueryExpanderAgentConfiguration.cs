namespace AgentMesh.Application.Configuration
{
    public class QueryExpanderAgentConfiguration
    {
        public const string SectionName = "Agents:QueryExpander";
        public const string AgentName = "QueryExpander";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
