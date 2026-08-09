namespace AgentMesh.Configuration
{
    public sealed class AppSettingsConfigurationDto
    {
        public InferenceProvidersConfigurationDto InferenceProviders { get; set; } = new();
        public LLMsConfigurationDto LLMs { get; set; } = new();
        public AgentsConfigurationDto Agents { get; set; } = new();
    }

    public sealed class InferenceProvidersConfigurationDto : Dictionary<string, InferenceProviderConfigurationDto>
    {
    }

    public sealed class InferenceProviderConfigurationDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public sealed class LLMsConfigurationDto : Dictionary<string, LLMConfigurationDto>
    {
    }

    public sealed class LLMConfigurationDto
    {
        public string Model { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public decimal CostPerMillionInputTokens { get; set; }
        public decimal CostPerMillionOutputTokens { get; set; }
    }

    public sealed class AgentsConfigurationDto : Dictionary<string, AgentConfigurationDto>
    {
    }

    public sealed class AgentConfigurationDto
    {
        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string? SystemPrompt { get; set; }
        public string? SystemPromptFile { get; set; }
    }
}
