namespace AgentMesh.Application.Models.AgentMemory
{
    /// <summary>
    /// Represents a memory item that is returned as part of a query result, including its confidence score. 
    /// </summary>
    public class AgentMemoryQueryResultItem : AgentMemoryItem
    {
        /// <summary>
        /// The confidence score of the memory item in relation to the query, typically ranging from 0.0 to 1.0.
        /// </summary>
        public double? Confidence { get; set; }
    }
}
