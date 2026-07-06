using AgentMesh.Models;

namespace AgentMesh.Models.AgentMemory
{
    /// <summary>
    /// Input model for saving conversation messages to the agent's memory.
    /// </summary>
    public class AgentMemorySaverConversationInput
    {
        /// <summary>
        /// Conversation history made of user and assistant messages.
        /// </summary>
        public IEnumerable<ContextMessage> ConversationHistory { get; set; } = [];
    }
}
