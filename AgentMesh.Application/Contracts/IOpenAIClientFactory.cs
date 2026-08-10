namespace AgentMesh.Application.Contracts
{
    public interface IOpenAIClientFactory
    {
        IOpenAIClient CreateOpenAIClient(string agentUniqueRole);
    }
}
