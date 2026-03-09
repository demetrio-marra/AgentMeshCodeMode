using AgentMesh.Models.IntentExtractor;

namespace AgentMesh.Application.Models
{
    public class ConversationSummarizerAgentInput
    {
        public IEnumerable<ContextMessage> Conversation { get; set; } = Array.Empty<ContextMessage>();
        public int CountOfMessagesToKeep { get; set; }
        public string SummaryLanguage { get; set; } = string.Empty;
    }
}
