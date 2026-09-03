namespace AgentMesh.Infrastructure.Cohere
{
    public class CohereV1RerankerServiceConfiguration
    {
        public const string SectionName = "CohereV1RerankerService";

        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
