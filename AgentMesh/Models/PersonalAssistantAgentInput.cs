namespace AgentMesh.Models
{
    public class PersonalAssistantAgentInput
    {
        public string Sentence { get; set; } = string.Empty;
        public string? Data { get; set; }
        public string TargetLanguage { get; set; } = string.Empty;
    }
}
