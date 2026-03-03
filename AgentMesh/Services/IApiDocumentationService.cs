namespace AgentMesh.Services
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
        /// <returns>A string containing the technical Javascript documentation for the specified API.</returns>
        Task<string> GetApiDocumentationAsync(string apiName);

        /// <summary>
        /// Retrieves multiple API documentation for the specified API names asynchronously. 
        /// </summary>
        /// <param name="apiNames">A collection of API names for which to retrieve documentation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary where the keys are API names and the values are the corresponding documentation strings.</returns>
        Task<Dictionary<string, string>> GetApiDocumentationAsync(IEnumerable<string> apiNames);
    }
}
