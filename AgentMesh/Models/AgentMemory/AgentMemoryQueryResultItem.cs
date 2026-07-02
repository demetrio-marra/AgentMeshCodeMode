namespace AgentMesh.Models.AgentMemory
{
    /// <summary>
    /// A class representing an item in the agent's memory, which can store information, observations, or any relevant data that the agent has encountered during its interactions or operations. Each memory item consists of the content of the memory and an associated confidence score that indicates the reliability or relevance of the information stored in that memory item.
    /// </summary>
    public class AgentMemoryQueryResultItem
    {
        /// <summary>
        /// The content of the memory item, which can be a piece of information, an observation, or any relevant data that the agent has encountered during its interactions or operations.
        /// </summary>
        public string Memory { get; set; } = string.Empty;

        /// <summary>
        /// Gets the confidence score associated with the result.
        /// </summary>
        public float? Confidence { get; set; }
    }
}
