namespace AgentMesh.Application.Services
{
    public interface IOpenAIClientFactory
    {
        IOpenAIClient CreateOpenAIClient(string model, string provider, string temperature, string systemPrompt);
    }
}
