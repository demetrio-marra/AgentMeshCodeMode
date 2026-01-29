using AgentMesh.Models;

namespace AgentMesh.Application.Models
{
    public class ConversationSummarizerAgentOutput : IAgentOutput
    {
        public string Summary { get; set; } = string.Empty;
        public IEnumerable<ContextMessage> NewConversation { get; set; } = Array.Empty<ContextMessage>();
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
