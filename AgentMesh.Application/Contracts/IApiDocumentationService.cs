using AgentMesh.Application.Models;

namespace AgentMesh.Application.Contracts
{
    /// <summary>
    /// Provides functionality for managing and accessing API technical Javascript documentation.
    /// </summary>
    public interface IApiDocumentationService
    {
        /// <summary>
        /// Retrieves the technical Javascript documentation for a specified API.
        /// </summary>
        /// <param name="apiName">The name of the API for which to retrieve documentation.</param>
        /// <returns>An <see cref="ApiDocumentation"/> object containing the technical Javascript documentation for the specified API.</returns>
        Task<ApiDocumentation> GetApiDocumentationAsync(string apiName);

        /// <summary>
        /// Retrieves multiple API documentation for the specified API names asynchronously. 
        /// </summary>
        /// <param name="apiNames">A collection of API names for which to retrieve documentation.</param>
        /// <returns>A collection of <see cref="ApiDocumentation"/> objects for the specified API names.</returns>
        Task<IEnumerable<ApiDocumentation>> GetApiDocumentationAsync(IEnumerable<string> apiNames);
    }
}
