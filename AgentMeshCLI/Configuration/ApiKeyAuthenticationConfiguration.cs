namespace AgentMesh.Configuration
{
    public sealed class ApiKeyAuthenticationConfiguration
    {
        public const string SectionName = "ApiAuth";

        public string ApiKey { get; set; } = string.Empty;
        public string HeaderName { get; set; } = "X-Api-Key";
    }
}
