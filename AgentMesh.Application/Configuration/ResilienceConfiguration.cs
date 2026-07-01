namespace AgentMesh.Application.Configuration
{
    public class ResilienceConfiguration
    {
        public const string SectionName = "Resilience";

        public int RetryCount { get; set; } = 3;
    }
}
