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
    }
}
