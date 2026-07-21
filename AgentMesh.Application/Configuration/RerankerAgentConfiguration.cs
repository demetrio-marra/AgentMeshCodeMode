namespace AgentMesh.Application.Configuration
{
    public class RerankerAgentConfiguration
    {
        public const string SectionName = "Agents:Reranker";
        public const string AgentName = "Reranker";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
