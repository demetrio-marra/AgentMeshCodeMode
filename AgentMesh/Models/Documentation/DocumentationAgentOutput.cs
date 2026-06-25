namespace AgentMesh.Models.Documentation
{
    public class DocumentationAgentOutput : IAgentOutput
    {
        public string? Content { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
