using AgentMesh.Services;

namespace AgentMesh.Models
{
    public interface IEWParameter
    {
        string Name { get; }
            
        IEWParameterSerializer Serializer { get; }

        IEWParameterSerializer DisplayValueSerializer { get; }

        string GetDisplayValue();

        string Serialize();
    }
}
