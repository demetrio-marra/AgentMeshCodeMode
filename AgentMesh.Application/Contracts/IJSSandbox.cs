using AgentMesh.Models.CodeSandbox;

namespace AgentMesh.Application.Contracts
{
    public interface IJSSandbox
    {
        Task<CodeSandboxOutput> RunCode(string agentId, string code);
    }
}
