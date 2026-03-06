namespace AgentMesh.Models
{
    public class BusinessRequirementsCreatorAgentInput
    {
        public string EnrichedUserRequest { get; set; } = string.Empty;
        public IEnumerable<string> ActionableRequirements { get; set; } = Enumerable.Empty<string>();
    }
}
