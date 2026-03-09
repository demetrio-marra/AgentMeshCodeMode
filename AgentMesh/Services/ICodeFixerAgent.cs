using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface ICodeFixerAgent : IExecutor<CodeFixerAgentInput, CodeFixerAgentOutput>
    {
    }
}
