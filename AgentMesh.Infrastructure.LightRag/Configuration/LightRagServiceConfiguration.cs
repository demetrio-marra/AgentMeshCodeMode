namespace AgentMesh.Infrastructure.LightRag.Configuration
{
    public class LightRagServiceConfiguration
    {
        public const string SectionName = "LightRagService";

        public string BaseUrl { get; set; } = string.Empty;
        public int MaxTopK { get; set; } = 10;
        public string? ApiKey { get; set; }
    }
}
