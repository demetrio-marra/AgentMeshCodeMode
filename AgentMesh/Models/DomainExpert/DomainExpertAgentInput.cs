namespace AgentMesh.Models.DomainExpert
{
    public class DomainExpertAgentInput
    {
        public string EnrichedUserRequest { get; set; } = string.Empty;

        /// <summary>
        /// Pre-fetched API documentation produced by <c>ISemanticSearchExecutor</c>.
        /// Empty string when no relevant documentation was found.
        /// </summary>
        public string ApiDocumentation { get; set; } = string.Empty;
    }
}
