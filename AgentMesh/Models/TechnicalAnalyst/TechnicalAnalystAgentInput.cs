using AgentMesh.Utils;

namespace AgentMesh.Models.TechnicalAnalyst
{
    public class TechnicalAnalystAgentInput
    {
        public string Intent { get; set; } = string.Empty;
        public string ConversationTopic { get; set; } = string.Empty;
        public IEnumerable<string> UserRequestedActions { get; set; } = [];
        public IEnumerable<string> UserProvidedData { get; set; } = [];
        public IEnumerable<string> UserPreferences { get; set; } = [];
        public IEnumerable<string> AgentMemories { get; set; } = [];
        public string KnowledgeBaseDocumentsContent { get; set; } = string.Empty;
        public string BusinessRequirements { get; set; } = string.Empty;

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Intent", Intent },
                { "Conversation topic", ConversationTopic },
                { "User requested actions", UserRequestedActions.Any() ? ListsFormatter.ToBulletList(UserRequestedActions) : "(No actions)" },
                { "User provided data", UserProvidedData.Any() ? ListsFormatter.ToBulletList(UserProvidedData) : "(No data)" },
                { "User preferences", UserPreferences.Any() ? ListsFormatter.ToBulletList(UserPreferences) : "(No user preferences)" },
                { "Agent memories", AgentMemories.Any() ? ListsFormatter.ToBulletList(AgentMemories) : "(No memories)" },
                { "Knowledge base documents content", $"Size: {KnowledgeBaseDocumentsContent.Length}" },
                { "Business requirements", BusinessRequirements }
            };
        }
    }
}
