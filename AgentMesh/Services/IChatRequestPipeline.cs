using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface IChatRequestPipeline : IEWPipeline
    {
        string FinalResponse { get; }
        IEnumerable<ContextMessage> InitialChatHistory { set; }
        string UserLastRequest { set; }
    }
}
