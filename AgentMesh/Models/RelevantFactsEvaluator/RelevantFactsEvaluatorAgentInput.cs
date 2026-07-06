using AgentMesh.Models;

namespace AgentMesh.Models.RelevantFactsEvaluator
{
    public class RelevantFactsEvaluatorAgentInput
    {
        public IEnumerable<ContextMessage> ConversationHistory { get; set; } = [];
    }
}

