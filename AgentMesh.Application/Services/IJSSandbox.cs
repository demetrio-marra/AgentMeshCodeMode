namespace AgentMesh.Application.Services
{
    public interface IJSSandbox
    {
        Task<string> RunCode(string agentId, string code);
    }
}
