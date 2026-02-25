using AgentMesh.Models;

namespace AgentMesh.Services
{
    /// <summary>
    /// A service that provides semantic search capabilities to retrieve relevant information based on actionable requirements.
    /// </summary>
    public interface ISemanticSearchService
    {
        /// <summary>
        /// Retrieves the available information that match the given requirements.
        /// </summary>
        /// <param name="agentRole">The role of the agent requesting the information.</param>
        /// <param name="actionableRequirements">The requirements that need to be fulfilled by the available information.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A collection of available information that match the given requirements.</returns>
        Task<IEnumerable<SemanticSearchResult>> SearchByActionableRequirements(string agentRole, IEnumerable<string> actionableRequirements, CancellationToken cancellationToken = default);
    }
}
