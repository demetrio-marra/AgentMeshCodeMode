namespace AgentMesh.Models
{
    public class PersonalAssistantAgentOutput : IAgentOutput
    {
        public string Response { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
