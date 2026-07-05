namespace AgentMesh.Models.RequirementsCollector
{
    public class RequirementsCollectorAgentInput
    {
        public string UserIntent { get; set; } = string.Empty;
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public IEnumerable<string> UserRequestDomains { get; set; } = [];
    }
}
