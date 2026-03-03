namespace AgentMesh.Models
{
    public class CoderAgentInput
    {
        public string BusinessRequirements { get; set; } = string.Empty;
        public IEnumerable<string> MentionedApis { get; set; } = Enumerable.Empty<string>();
    }
}
