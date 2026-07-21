using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.FunctionalAnalyst
{
    public class FunctionalAnalystAgentOutput : IAgentOutput
    {
        public string BusinessRequirements { get; set; } = string.Empty;
        public required bool RequestRejected { get; set; }
        public string? ReasonOfRejection { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Business requirements", BusinessRequirements },
                { "Request rejected", RequestRejected.ToString() },
                { "Reason of rejection", ReasonOfRejection ?? string.Empty }
            };
        }
    }
}
