namespace AgentMesh.Models.AgentMemory
{
    /// <summary>
    /// Wraps the input parameters required for searching the agent's memory.
    /// </summary>
    public class AgentMemoryRetrieverInput
    {
        /// <summary>
        /// The sentence or query that the agent will use to search its memory. This should be a natural language request that describes what the user is looking for in the agent's memory.
        /// </summary>
        public string Query { get; set; } = string.Empty;
    }
}
