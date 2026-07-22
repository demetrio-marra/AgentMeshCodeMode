using AgentMesh.Models.ChatMessages;

namespace AgentMesh.Application.Models.ConversationSummarization
{
    public class ConversationSummarizerAgentInput
    {
        public IEnumerable<ContextMessage> Conversation { get; set; } = [];
        public int CountOfMessagesToKeep { get; set; }
        public string SummaryLanguage { get; set; } = string.Empty;

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Conversation", Conversation.Any() ? $"Messages count: {Conversation.Count()}" : "(No conversation)" },
                { "Count of messages to keep", CountOfMessagesToKeep.ToString() },
                { "Summary language", SummaryLanguage }
            };
        }
    }
}
