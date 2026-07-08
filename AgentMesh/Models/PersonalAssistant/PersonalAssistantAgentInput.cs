namespace AgentMesh.Models.PersonalAssistant
{
    public class PersonalAssistantAgentInput
    {
        public string? Data { get; set; }
        public bool ExecutionError { get; set; }
        public string? LanguageOfTheUser { get; set; } = string.Empty;
        public string OriginalUserRequest { get; set; } = string.Empty;
        public string CanonicalizedIntent { get; set; } = string.Empty;
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public IEnumerable<string> UserPreferences { get; set; } = [];
        public IEnumerable<string> Memories { get; set; } = [];
    }
}
