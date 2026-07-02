namespace AgentMesh.Infrastructure.QDrant.Configuration
{
    public class DocumentsCacheServiceConfiguration
    {
        public const string SectionName = "DocumentsCacheService";

        public int ExpirationMinutes { get; set; } = 30;
    }
}
