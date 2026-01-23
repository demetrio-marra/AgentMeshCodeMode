namespace AgentMesh.Models
{
    public class CodeFixerAgentInput
    {
        public string CodeToFix { get; set; } = string.Empty;

        public IEnumerable<string> Issues { get; set; } = [];
    }
}
