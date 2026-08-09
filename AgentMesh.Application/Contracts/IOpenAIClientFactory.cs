namespace AgentMesh.Application.Contracts
{
    public interface IOpenAIClientFactory
    {
        IOpenAIClient CreateOpenAIClient(string model, string provider, string temperature, string systemPrompt);
        IOpenAIClient CreateOpenAIClient(string agentUniqueRole);
    }
}
