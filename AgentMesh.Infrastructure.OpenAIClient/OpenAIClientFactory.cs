using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;

namespace AgentMesh.Infrastructure.OpenAIClient
{
    public class OpenAIClientFactory(IEnumerable<AgentFlatConfigurationRecord> agentFlatConfigurationRecords,
        OpenAIClientFactoryConfiguration configuration) : 
        IOpenAIClientFactory
    {

        public IOpenAIClient CreateOpenAIClient(string model, string provider, string temperature, string systemPrompt)
        {
            var apikey = configuration[provider].ApiKey;
            var endpoint = configuration[provider].Endpoint;
            return new OpenAIClient(model, apikey, endpoint, temperature, systemPrompt);
        }

        public IOpenAIClient CreateOpenAIClient(string agentUniqueRole)
        {
            var cfg = GetAgentConfiguration(agentUniqueRole);
            return new OpenAIClient(cfg.ProviderModelName, 
                cfg.ProviderApiKey,
                cfg.ProviderEndpoint, 
                cfg.Temperature,
                cfg.SystemPrompt);
        }

        private AgentFlatConfigurationRecord GetAgentConfiguration(string agentUniqueRole)
        {
            var cfg = agentFlatConfigurationRecords.Single(x => string.Compare(agentUniqueRole, x.AgentUniqueRole, true) == 0);
            return cfg;
        }
    }
}
