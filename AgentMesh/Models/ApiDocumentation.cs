namespace AgentMesh.Models
{
    /// <summary>
    /// Represents technical Javascript documentation for a specific API, including the API name and its corresponding documentation content.
    /// </summary>
    public class ApiDocumentation
    {
        /// <summary>
        /// The unique name of the API for which this documentation fragment applies. 
        /// This should correspond to the identifier used to reference the API in the system, allowing agents to retrieve and utilize the correct documentation when interacting with that API.
        /// </summary>
        public string ApiName { get; set; } = string.Empty;

        /// <summary>
        /// The technical Javascript documentation content for the specified API.
        /// This should include relevant information such as method signatures, usage examples, parameter descriptions, and any other details necessary for an agent to effectively utilize the API in its operations. 
        /// The content should be formatted in a way that is easily parsable and understandable by the agents consuming it.
        /// </summary>
        public string Documentation { get; set; } = string.Empty;
    }
}
