using AgentMesh.Models.CodeSmellDetector;

namespace AgentMesh.Application.Contracts
{
    public interface ICodeSmellDetector
    {
        Task<CodeSmellDetectorOutput> DetectCodeSmellsAsync(CodeSmellDetectorInput input);
    }
}
