namespace AgentMesh.Models
{
    public class IntentExtractorAgentInput
    {
        public List<ContextMessage> ContextMessages { get; set; } = new List<ContextMessage>();
        public string UserLastRequest { get; set; } = string.Empty;
    }
}
