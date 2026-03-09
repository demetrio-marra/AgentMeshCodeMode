using AgentMesh.Models.CodeExecutionFailuresDetector;

namespace AgentMesh.Services
{
    public interface ICodeExecutionFailuresDetectorAgent : IExecutor<CodeExecutionFailuresDetectorAgentInput, CodeExecutionFailuresDetectorAgentOutput>
    {
    }
}
