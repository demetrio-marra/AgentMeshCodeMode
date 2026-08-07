using AgentMesh.Services;

namespace AgentMesh.Models.Workflows
{
    public interface IEWParameter
    {
        string Name { get; }

        bool IsConversationHistoryParameter { get; }
          
        bool IsUserCurrentRequestParameter { get; }

        bool IsResponseForUserParameter { get; }

        Type ParameterType { get; }

        IEWParameterSerializer DisplayValueSerializer { get; }

        string GetDisplayValue();
    }
}
