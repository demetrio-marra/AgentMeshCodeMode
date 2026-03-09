namespace AgentMesh.Models
{
    /// <summary>
    /// Wrapper class for the output of the agent memory executor, which contains a list of memory items that match the search query. Each memory item includes the content of the memory and an associated confidence score indicating the reliability or relevance of the information stored in that memory item.
    /// </summary>
    public class AgentMemoryExecutorSearchMemoryOutput
    {
        /// <summary>
        /// The list of memory items that match the search query, each containing the content of the memory and an associated confidence score indicating the reliability or relevance of the information stored in that memory item.
        /// </summary>
        public IEnumerable<AgentMemoryItem> Items { get; set; } = Enumerable.Empty<AgentMemoryItem>();
    }
}
