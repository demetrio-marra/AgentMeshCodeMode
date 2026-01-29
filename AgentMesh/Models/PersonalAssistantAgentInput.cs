namespace AgentMesh.Models
{
    public class PersonalAssistantAgentInput
    {
        public string? Data { get; set; }
        public string OutputLanguage { get; set; } = string.Empty;
        public string UserRequest { get; set; } = string.Empty;
        public string RequestContext { get; set; } = string.Empty;
    }
}
