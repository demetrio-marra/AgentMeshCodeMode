namespace AgentMesh.Application.Models
{
    public class AgentTokenUsageEntry
    {
        public string AgentName { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }
}
