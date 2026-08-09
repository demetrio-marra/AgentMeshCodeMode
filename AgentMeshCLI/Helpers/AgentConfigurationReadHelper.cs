using AgentMesh.Application.Configuration;
using AgentMesh.Configuration;

namespace AgentMesh.Helpers
{
    public static class AgentConfigurationReadHelper
    {
        public static IEnumerable<AgentFlatConfigurationRecord> ReadAgentConfigurations(AppSettingsConfigurationDto appSettings, string basePath)
        {
            foreach (var (agentName, agentConfiguration) in appSettings.Agents)
            {
                if (string.IsNullOrWhiteSpace(agentConfiguration.LLM))
                {
                    throw new InvalidOperationException($"Agent configuration '{agentName}' is missing the 'LLM' value.");
                }

                if (!appSettings.LLMs.TryGetValue(agentConfiguration.LLM, out var llmConfiguration))
                {
                    throw new InvalidOperationException($"LLM configuration '{agentConfiguration.LLM}' referenced by agent '{agentName}' was not found.");
                }

                if (!appSettings.InferenceProviders.TryGetValue(llmConfiguration.Provider, out var providerConfiguration))
                {
                    throw new InvalidOperationException($"Inference provider '{llmConfiguration.Provider}' referenced by LLM '{agentConfiguration.LLM}' was not found.");
                }

                yield return new AgentFlatConfigurationRecord
                {
                    AgentUniqueRole = agentName,
                    ProviderName = llmConfiguration.Provider,
                    ProviderEndpoint = providerConfiguration.Endpoint,
                    ProviderApiKey = providerConfiguration.ApiKey,
                    ProviderModelName = llmConfiguration.Model,
                    LLMClass = agentConfiguration.LLM,
                    LLMClassCostPerMillionInputTokens = llmConfiguration.CostPerMillionInputTokens,
                    LLMClassCostPerMillionOutputTokens = llmConfiguration.CostPerMillionOutputTokens,
                    Temperature = agentConfiguration.ModelTemperature,
                    SystemPrompt = ResolveSystemPrompt(agentConfiguration, basePath)
                };
            }
        }

        private static string ResolveSystemPrompt(AgentConfigurationDto agentConfiguration, string basePath)
        {
            if (!string.IsNullOrWhiteSpace(agentConfiguration.SystemPrompt))
            {
                return agentConfiguration.SystemPrompt;
            }

            if (string.IsNullOrWhiteSpace(agentConfiguration.SystemPromptFile))
            {
                return string.Empty;
            }

            var promptFilePath = Path.IsPathRooted(agentConfiguration.SystemPromptFile)
                ? agentConfiguration.SystemPromptFile
                : Path.Combine(basePath, agentConfiguration.SystemPromptFile);

            if (!File.Exists(promptFilePath))
            {
                throw new FileNotFoundException($"System prompt file not found: {promptFilePath}");
            }

            return File.ReadAllText(promptFilePath);
        }
    }
}
