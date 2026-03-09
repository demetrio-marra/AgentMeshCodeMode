namespace AgentMesh.Models.BusinessRequirementsCreator
{
    public class BusinessRequirementsCreatorAgentInput
    {
        public string EnrichedUserRequest { get; set; } = string.Empty;

        /// <summary>
        /// Pre-fetched API documentation produced by <c>ISemanticSearchExecutor</c>.
        /// Empty string when no relevant documentation was found.
        /// </summary>
        public string ApiDocumentation { get; set; } = string.Empty;
    }
}
