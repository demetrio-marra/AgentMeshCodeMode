namespace AgentMesh.Models
{
    public class BusinessAdvisorAgentInput
    {
        public string UserRequest { get; set; } = string.Empty;
        public string RequestContext { get; set; } = string.Empty;
        public IEnumerable<string> ActionableRequirements { get; set; } = Enumerable.Empty<string>();
    }
}
