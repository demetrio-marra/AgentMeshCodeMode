namespace AgentMesh.Models.BusinessAdvisor
{
    public class BusinessAdvisorAgentInput
    {
        public string EnrichedUserRequest { get; set; } = string.Empty;

        /// <summary>
        /// Pre-fetched documentation produced by <c>ISemanticSearchExecutor</c>.
        /// Empty string when no relevant documentation was found.
        /// </summary>
        public string Documentation { get; set; } = string.Empty;
    }
}
