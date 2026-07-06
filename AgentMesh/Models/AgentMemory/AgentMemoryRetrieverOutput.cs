namespace AgentMesh.Models.AgentMemory
{
    /// <summary>
    /// Output of the agent memory retriever, containing the matching memory items.
    /// </summary>
    public class AgentMemoryRetrieverOutput
    {
        /// <summary>
        /// The list of memory items that match the search query.
        /// </summary>
        public IEnumerable<AgentMemoryQueryResultItem> Items { get; set; } = [];
    }
}
