namespace AgentMesh.Models.PersonalAssistant
{
    public class PersonalAssistantAgentInput
    {
        public string? Data { get; set; }
        public bool RequestFailed { get; set; }
        public string? RequestFailureReason { get; set; }
        public string? LanguageOfTheUser { get; set; } = string.Empty;
        public string CanonicalizedIntent { get; set; } = string.Empty;
        public string ConversationTopic { get; set; } = string.Empty;
        public IEnumerable<string> UserPreferences { get; set; } = [];
        public IEnumerable<string> UserProvidedData { get; set; } = [];
        public IEnumerable<string> UserRequestedActions { get; set; } = [];
        public IEnumerable<string> Memories { get; set; } = [];
    }
}
