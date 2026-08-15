using AgentMesh.Models;

namespace AgentMesh.Application.Models.Conversation
{
    public class ConversationContext
    {
        public IEnumerable<ContextMessage> Conversation { get; set; } = [];

        /// <summary>
        /// Used to track tokens count and leverage strategies to reduce the context size if needed.
        /// </summary>
        public int TokensCount { get; set; }
    }
}
