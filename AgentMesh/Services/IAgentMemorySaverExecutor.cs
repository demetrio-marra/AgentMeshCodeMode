using AgentMesh.Models.AgentMemory;

namespace AgentMesh.Services
{
    public interface IAgentMemorySaverExecutor
    {
        /// <summary>
        /// Saves conversation messages to the agent's memory.
        /// </summary>
        /// <param name="input">The conversation history to save to memory.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(AgentMemorySaverConversationInput input);
    }
}
