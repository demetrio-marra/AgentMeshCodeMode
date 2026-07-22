namespace AgentMesh.Application.Configuration
{
    public class KnowledgeBaseQueryExpanderAgentConfiguration
    {
        public const string SectionName = "Agents:KnowledgeBaseQueryExpander";
        public const string AgentName = "KnowledgeBaseQueryExpander";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
