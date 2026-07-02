using static AgentMesh.Models.IntentExtractor.IntentExtractorAgentOutput;

namespace AgentMesh.Models.IntentExtractor
{
    public class IntentExtractorAgentInput
    {
        public List<ContextMessage> ContextMessages { get; set; } = [];
        public string UserLastRequest { get; set; } = string.Empty;
    }
}
