using AgentMesh.Models;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Utils;

namespace AgentMesh.Application.Models
{
    public class ConversationSummarizerAgentOutput : IAgentOutput
    {
        public string Summary { get; set; } = string.Empty;
        public IEnumerable<ContextMessage> NewConversation { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Summary", Summary },
                { "New conversation", NewConversation.Any() ? $"Messages count: {NewConversation.Count()}" : "(No conversation)" }
            };
        }
    }
}
