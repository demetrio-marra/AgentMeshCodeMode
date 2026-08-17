namespace AgentMesh.Application.Configuration
{
    public readonly record struct AgentFlatConfigurationRecord
    {
        public readonly string AgentUniqueRole { get; init; }

        public readonly string ProviderName { get; init; }
        public readonly string ProviderEndpoint { get; init; }
        public readonly string ProviderApiKey { get; init; }
        public readonly string ProviderModelName { get; init; }

        public readonly string LLMClass { get; init; }
        public readonly decimal LLMClassCostPerMillionInputTokens { get; init; }
        public readonly decimal LLMClassCostPerMillionOutputTokens { get; init; }
        
        public readonly string Temperature { get; init; }
        public readonly string SystemPrompt { get; init; }
    }
}
