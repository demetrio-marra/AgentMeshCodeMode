namespace AgentMesh.Models.ApiDocumentation
{
    /// <summary>
    /// Output of the API documentation executor, containing the retrieved documentation
    /// as a single pre-formatted string ready for injection into an agent prompt.
    /// </summary>
    public class ApiDocumentationExecutorOutput
    {
        /// <summary>
        /// The concatenated API documentation for the requested APIs.
        /// Empty string when no documentation was found.
        /// </summary>
        public string ApiDocumentation { get; set; } = string.Empty;
    }
}
