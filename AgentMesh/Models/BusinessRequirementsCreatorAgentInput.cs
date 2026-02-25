namespace AgentMesh.Models
{
    public class BusinessRequirementsCreatorAgentInput
    {
        public string UserRequest { get; set; } = string.Empty;
        public string RequestContext { get; set; } = string.Empty;
        public IEnumerable<string> ActionableRequirements { get; set; } = Enumerable.Empty<string>();
    }
}
