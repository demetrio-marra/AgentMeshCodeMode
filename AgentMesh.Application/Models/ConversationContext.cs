using AgentMesh.Models;

namespace AgentMesh.Application.Models
{
    public class ConversationContext
    {
        public IEnumerable<ContextMessage> Conversation { get; set; } = Enumerable.Empty<ContextMessage>();
        public int TokensCount { get; set; }
    }
}
