using System.Collections.Generic;

namespace AgentMesh.Infrastructure.OpenAIClient
{
    public class OpenAIClientFactoryConfiguration : Dictionary<string, OpenAIClientProviderConfiguration>
    {
        public const string SectionName = "InferenceProviders";
    }

    public class OpenAIClientProviderConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
    }
}
