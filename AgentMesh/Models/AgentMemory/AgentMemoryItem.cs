namespace AgentMesh.Models.AgentMemory
{
    /// <summary>
    /// A class representing an item in the agent's memory, which can store information, observations, or any relevant data that the agent has encountered during its interactions or operations. 
    /// </summary>
    public class AgentMemoryItem
    {
        /// <summary>
        /// The content of the memory item, which can be a piece of information, an observation, or any relevant data that the agent has encountered during its interactions or operations.
        /// </summary>
        public string Memory { get; set; } = string.Empty;
    }
}
