namespace AgentMesh.Models.ApiDocumentation
{
    /// <summary>
    /// Input for the API documentation executor, containing the API names to fetch documentation for.
    /// </summary>
    public class ApiDocumentationExecutorInput
    {
        /// <summary>
        /// The set of API names for which to retrieve documentation.
        /// </summary>
        public IEnumerable<string> MentionedApis { get; set; } = Enumerable.Empty<string>();
    }
}
