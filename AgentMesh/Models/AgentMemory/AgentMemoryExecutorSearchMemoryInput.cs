namespace AgentMesh.Models.AgentMemory
{
    /// <summary>
    /// Wraps the input parameters required for searching the agent's memory. This class contains a single property, Query, which represents the query or prompt that the agent will use to search its memory. The agent will utilize this query to retrieve relevant memory items that match the criteria specified in the query, allowing it to access and utilize past information or experiences stored in its memory to inform its current decision-making or actions.
    /// </summary>
    public class AgentMemoryExecutorSearchMemoryInput
    {
        /// <summary>
        /// Represents the query or prompt that the agent will use to search its memory. This query is typically a string that contains the information or context that the agent is looking for in its memory. The agent will use this query to retrieve relevant memory items that match the criteria specified in the query, allowing it to access and utilize past information or experiences stored in its memory to inform its current decision-making or actions.
        /// </summary>
        public string Query { get; set; } = string.Empty;
    }
}
