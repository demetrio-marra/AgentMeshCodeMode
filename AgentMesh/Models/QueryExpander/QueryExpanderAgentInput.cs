using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Models.QueryExpander
{
    public class QueryExpanderAgentInput
    {
        public StructuredUserRequest StructuredUserRequest { get; set; } = new();
    }
}
