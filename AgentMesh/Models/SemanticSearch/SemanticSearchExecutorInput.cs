namespace AgentMesh.Models.SemanticSearch
{
    /// <summary>
    /// Input for the semantic search executor, containing the actionable requirements to search for
    /// and the optional role of the requesting agent.
    /// </summary>
    public class SemanticSearchExecutorInput
    {
        /// <summary>
        /// The actionable requirements used to drive the semantic search.
        /// </summary>
        public IEnumerable<string> ActionableRequirements { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// Optional role of the agent requesting the search, used to scope or filter results.
        /// </summary>
        public string? AgentRole { get; set; }
    }
}
