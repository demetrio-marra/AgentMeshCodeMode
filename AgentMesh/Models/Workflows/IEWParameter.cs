namespace AgentMesh.Models.Workflows
{
    public interface IEWParameter
    {
        string Name { get; }

        bool IsConversationHistoryParameter { get; }
          
        bool IsUserCurrentRequestParameter { get; }

        bool IsResponseForUserParameter { get; }

        string DisplayValue { get; }

        string? RawSerializedValue { get; }
    }
}
