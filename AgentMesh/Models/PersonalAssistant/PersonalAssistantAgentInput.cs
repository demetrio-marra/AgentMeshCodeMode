using AgentMesh.Utils;

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

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Data", Data ?? string.Empty },
                { "Request failed", RequestFailed.ToString() },
                { "Request failure reason", RequestFailureReason ?? string.Empty },
                { "Language of the user", LanguageOfTheUser ?? string.Empty },
                { "Canonicalized intent", CanonicalizedIntent },
                { "Conversation topic", ConversationTopic },
                { "User preferences", UserPreferences.Any() ? ListsFormatter.ToBulletList(UserPreferences) : "(No user preferences)" },
                { "User provided data", UserProvidedData.Any() ? ListsFormatter.ToBulletList(UserProvidedData) : "(No user provided data)" },
                { "User requested actions", UserRequestedActions.Any() ? ListsFormatter.ToBulletList(UserRequestedActions) : "(No user requested actions)" },
                { "Memories", Memories.Any() ? ListsFormatter.ToBulletList(Memories) : "(No memories)" }
            };
        }
    }
}
