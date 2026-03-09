namespace AgentMesh.Models.CodeFixer
{
    public class CodeFixerAgentInput
    {
        public string CodeToFix { get; set; } = string.Empty;

        public IEnumerable<string> Issues { get; set; } = [];
    }
}
