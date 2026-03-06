namespace AgentMesh.Infrastructure.AgentMemory.Configuration
{
    public class AgentMemoryServiceConfiguration
    {
        public const string SectionName = "AgentMemoryService";

        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
