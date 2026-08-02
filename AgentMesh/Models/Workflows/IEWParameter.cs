using AgentMesh.Services;

namespace AgentMesh.Models.Workflows
{
    public interface IEWParameter
    {
        string Name { get; }

        bool IsConversationHistoryParameter { get; }
          
        bool IsUserCurrentRequestParameter { get; }

        bool IsResponseForUserParameter { get; }

        IEWParameterSerializer DisplayValueSerializer { get; }

        IEWParameterSerializer Serializer { get; }

        string GetDisplayValue();

        string GetSerializedValue();
    }
}
