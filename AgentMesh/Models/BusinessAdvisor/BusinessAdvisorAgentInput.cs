namespace AgentMesh.Models.BusinessAdvisor
{
    public class BusinessAdvisorAgentInput
    {
        public string EnrichedUserRequest { get; set; } = string.Empty;
        public IEnumerable<string> ActionableRequirements { get; set; } = Enumerable.Empty<string>();
    }
}
