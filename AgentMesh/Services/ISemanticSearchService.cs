namespace AgentMesh.Services
{
    public interface ISemanticSearchService
    {
        /// <summary>
        /// Retrieves the available information that match the given requirements.
        /// </summary>
        /// <param name="actionableRequirements">The requirements that need to be fulfilled by the available information.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A collection of available information that match the given requirements.</returns>
        Task<IEnumerable<string>> SearchAsync(IEnumerable<string> actionableRequirements, CancellationToken cancellationToken = default);
    }
}
