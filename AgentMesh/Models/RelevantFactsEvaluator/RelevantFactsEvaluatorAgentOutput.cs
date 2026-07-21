using AgentMesh.Utils;

namespace AgentMesh.Models.RelevantFactsEvaluator
{
    public class RelevantFactsEvaluatorAgentOutput : IAgentOutput
    {
        public IEnumerable<string> RelevantUserMessages { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Relevant user messages", RelevantUserMessages.Any() ? ListsFormatter.ToBulletList(RelevantUserMessages) : "(No relevant user messages)" }
            };
        }
    }
}

