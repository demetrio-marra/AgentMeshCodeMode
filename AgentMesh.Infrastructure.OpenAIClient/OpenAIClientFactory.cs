using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;

namespace AgentMesh.Infrastructure.OpenAIClient
{
    public class OpenAIClientFactory(IEnumerable<AgentFlatConfigurationRecord> agentFlatConfigurationRecords) : 
        IOpenAIClientFactory
    {
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
