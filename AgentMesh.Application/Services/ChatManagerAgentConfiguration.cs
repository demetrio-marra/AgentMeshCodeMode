namespace AgentMesh.Application.Services
{
    public class ChatManagerAgentConfiguration
    {
        public const string SectionName = "Agents:ChatManager";
        public const string AgentName = "ChatManager";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }

        public string SummaryPrompt { get; set; } = "Summary this chat history in max 200 words";
        public int SummaryTokenThreshold { get; set; } = 1500;
        public int NumMessageToPreseve { get; set; } = 5;
    }
}
