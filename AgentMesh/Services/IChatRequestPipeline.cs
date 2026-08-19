using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface IChatRequestPipeline : IEWPipeline
    {
        string FinalResponse { get; }
        void SetParameterInitialValues(string userLastRequest, IEnumerable<ContextMessage> initialChatHistory, DateTime requestDateTime);
    }
}
