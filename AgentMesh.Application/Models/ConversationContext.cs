using AgentMesh.Models;

namespace AgentMesh.Application.Models
{
    public class ConversationContext
    {
        public IEnumerable<ContextMessage> Conversation { get; set; } = [];
        public int TokensCount { get; set; }
    }
}
