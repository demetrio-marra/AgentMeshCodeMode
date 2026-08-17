using AgentMesh.Services;

namespace AgentMesh.Models
{
    public interface IEWParameter
    {
        string Name { get; }

        bool IsConversationHistoryParameter { get; }
          
        bool IsUserCurrentRequestParameter { get; }

        bool IsResponseForUserParameter { get; }

        IEWParameterSerializer Serializer { get; }

        IEWParameterSerializer DisplayValueSerializer { get; }

        string GetDisplayValue();

        string Serialize();
    }
}
