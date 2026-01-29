namespace AgentMesh.Application.Services
{
    public class ConversationSummarizerAgentConfiguration
    {
        public const string SectionName = "Agents:ConversationSummarizer";
        public const string AgentName = "ConversationSummarizer";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
