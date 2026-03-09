namespace AgentMesh.Application.Contracts
{
    public interface IJSSandbox
    {
        Task<string> RunCode(string agentId, string code);
    }
}
