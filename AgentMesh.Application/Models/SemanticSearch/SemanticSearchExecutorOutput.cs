namespace AgentMesh.Application.Models.SemanticSearch
{
    /// <summary>
    /// Output of the semantic search executor, containing the retrieved documentation
    /// as a single pre-formatted string ready for injection into an agent prompt.
    /// </summary>
    public class SemanticSearchExecutorOutput
    {
        /// <summary>
        /// The concatenated API documentation found by the semantic search.
        /// Empty string when no relevant documentation was found.
        /// </summary>
        public string ApiDocumentation { get; set; } = string.Empty;

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Api documentation", ApiDocumentation }
            };
        }
    }
}
