namespace AgentMesh.Application.Models
{
    /// <summary>
    /// Wraps the input parameters required for searching the agent's memory.
    /// </summary>
    public class AgentMemoryRetrieverInput
    {
        /// <summary>
        /// The query or prompt that the agent will use to search its memory.
        /// </summary>
        public string Query { get; set; } = string.Empty;
    }
}
