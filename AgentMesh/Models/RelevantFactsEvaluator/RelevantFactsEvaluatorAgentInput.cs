using AgentMesh.Models.ChatMessages;
using AgentMesh.Utils;

namespace AgentMesh.Models.RelevantFactsEvaluator
{
    public class RelevantFactsEvaluatorAgentInput
    {
        public IEnumerable<ContextMessage> ConversationHistory { get; set; } = [];

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Conversation history", ConversationHistory.Any() ? $"Messages count: {ConversationHistory.Count()}" : "(No conversation history)" }
            };
        }
    }
}

