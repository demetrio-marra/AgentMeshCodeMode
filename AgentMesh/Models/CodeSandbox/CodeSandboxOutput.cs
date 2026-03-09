namespace AgentMesh.Models.CodeSandbox
{
    public class CodeSandboxOutput : IAgentOutput
    {
        public string Result { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
