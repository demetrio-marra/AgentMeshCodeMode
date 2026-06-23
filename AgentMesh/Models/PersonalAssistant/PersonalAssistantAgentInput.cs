namespace AgentMesh.Models.PersonalAssistant
{
    public class PersonalAssistantAgentInput
    {
        public string? Data { get; set; }
        public string? LanguageOfTheUser { get; set; } = string.Empty;
        public string EnrichedUserRequest { get; set; } = string.Empty;
    }
}
