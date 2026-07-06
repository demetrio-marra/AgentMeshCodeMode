using AgentMesh.Models;
using AgentMesh.Models.AgentMemory;

namespace AgentMesh.Application.Contracts
{
    /// <summary>
    /// Agent memory service for persisting conversation messages and querying memory.
    /// </summary>
    public interface IAgentMemoryService
    {
        /// <summary>
        /// Adds conversation messages to the agent's memory.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="conversationHistory">The conversation messages to store.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddConversationHistory(string userId, IEnumerable<ContextMessage> conversationHistory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves agent memory items matching the specified query for the given user.
        /// </summary>
        /// <param name="userId">The identifier of the user whose memory items are queried.</param>
        /// <param name="query">The search query used to filter memory items.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation, containing a collection of matching agent memory items.</returns>
        Task<IEnumerable<AgentMemoryQueryResultItem>> Query(string userId, string query, CancellationToken cancellationToken = default);
    }
}
