using AgentMesh.Models.CodeSmellDetector;

namespace AgentMesh.Application.Services
{
    public interface ICodeSmellDetector
    {
        Task<CodeSmellDetectorOutput> DetectCodeSmellsAsync(CodeSmellDetectorInput input);
    }
}
