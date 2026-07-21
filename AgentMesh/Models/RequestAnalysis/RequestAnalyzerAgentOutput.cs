using AgentMesh.Utils;

namespace AgentMesh.Models.RequestAnalysis
{
    public class RequestAnalyzerAgentOutput : StructuredUserRequest, IAgentOutput
    {
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Intent", Intent },
                { "Intent category", IntentCategory.ToString() },
                { "Conversation topic", ConversationTopic ?? string.Empty },
                { "User requested actions", UserRequestedActions.Any() ? ListsFormatter.ToBulletList(UserRequestedActions) : "(No requested actions)" },
                { "User provided data", UserProvidedData.Any() ? ListsFormatter.ToBulletList(UserProvidedData) : "(No provided data)" },
                { "User preferences", UserPreferences.Any() ? ListsFormatter.ToBulletList(UserPreferences) : "(No user preferences)" },
                { "Missing values", MissingValues.Any() ? ListsFormatter.ToBulletList(MissingValues) : "(No missing values)" },
                { "Language of the user", LanguageOfTheUser }
            };
        }
    }
}
