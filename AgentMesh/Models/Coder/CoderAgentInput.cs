namespace AgentMesh.Models.Coder
{
    public class CoderAgentInput
    {
        public string BusinessRequirements { get; set; } = string.Empty;

        /// <summary>
        /// Pre-fetched API documentation produced by <c>IApiDocumentationExecutor</c>.
        /// Empty string when no relevant documentation was found.
        /// </summary>
        public string ApiDocumentation { get; set; } = string.Empty;
    }
}
