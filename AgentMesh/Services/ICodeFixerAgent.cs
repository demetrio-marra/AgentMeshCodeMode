using AgentMesh.Models.CodeFixer;

namespace AgentMesh.Services
{
    public interface ICodeFixerAgent : IExecutor<CodeFixerAgentInput, CodeFixerAgentOutput>
    {
    }
}
