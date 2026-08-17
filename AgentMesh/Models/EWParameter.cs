using AgentMesh.Services;

namespace AgentMesh.Models
{
    public abstract class EWParameter<T> : IEWParameter
    {
        public string Name { get; init; } = string.Empty;

        public T? ParameterValue { get; set; }

        public IEWParameterSerializer DisplayValueSerializer { get; init; } = new DefaultEWParameterSerializer();

        public IEWParameterSerializer Serializer { get; init; } = new DefaultEWParameterSerializer();

        public string GetDisplayValue()
        {
            return DisplayValueSerializer.Serialize(ParameterValue);
        }

        public string Serialize()
        {
            return Serializer.Serialize(ParameterValue);
        }
    }
}
