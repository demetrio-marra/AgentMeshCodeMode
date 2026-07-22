using AgentMesh.Models.ChatMessages;

namespace AgentMesh.Application.Models.CostsAnalysis
{
    public class ConversationContext
    {
        public IEnumerable<ContextMessage> Conversation { get; set; } = [];
        public int TokensCount { get; set; }
    }
}
