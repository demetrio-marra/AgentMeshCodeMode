using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Models.QueryExpander
{
    public class QueryExpanderAgentInput
    {
        public StructuredUserRequest StructuredUserRequest { get; set; } = new();

        /// <summary>
        /// Flag to instruct the agent to generate queries for the HYDE (Hypothetical Document Embedding) approach, which involves generating hypothetical documents to improve search results and information retrieval.
        /// </summary>
        public bool GenerateHydeQueries { get; set; }

        /// <summary>
        /// fixed md file documenting the query types and their expected structure, which can be used as a reference for generating queries in a consistent format.
        /// </summary>
        public string? QmdQueryTypesReference { get; set; }
    }
}
