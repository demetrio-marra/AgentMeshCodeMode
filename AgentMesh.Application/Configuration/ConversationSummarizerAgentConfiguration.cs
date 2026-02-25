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
        public int SummaryTokenThreshold { get; set; } = 1500;
        public int NumMessageToPreseve { get; set; } = 5;
        public string SummarizeLanguage { get; set; } = string.Empty;
    }
}
