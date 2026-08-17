using AgentMesh.Application.Configuration;
using AgentMesh.Models;

namespace AgentMesh.Application.Models.Conversation
{
    public class ConversationContext(ConversationSummarizationConfiguration conversationSummarizerAgentConfiguration)
    {
        public IEnumerable<ContextMessage> Conversation { get; set; } = [];

        /// <summary>
        /// Used to track tokens count and leverage strategies to reduce the context size if needed.
        /// </summary>
        public int TokensCount { get; set; }

        public bool RequiresSummarization => TokensCount >= conversationSummarizerAgentConfiguration.SummaryTokenThreshold;
    }
}
