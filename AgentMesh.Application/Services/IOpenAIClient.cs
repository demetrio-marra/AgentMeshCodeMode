using AgentMesh.Application.Models;
using AgentMesh.Models;

namespace AgentMesh.Application.Services
{
    public interface IOpenAIClient
    {
        Task<OpenAIClientResponse> GenerateResponseAsync(IEnumerable<string> userInput);
        Task<OpenAIClientResponse> GenerateResponseAsync(IEnumerable<AgentMessage> messages);
    }
}
