using AgentMesh.Services;

namespace AgentMesh.Models
{
    public abstract class BaseEWParameterConfiguration<T>() : IEWParameterConfiguration
    {
        public Type ValueType => typeof(T);
        public abstract string Name { get; }
        public virtual IEWParameterSerializer DisplayValueSerializer => new DefaultEWParameterSerializer();
        public virtual IEWParameterSerializer ValueSerializer => new DefaultEWParameterSerializer();
        public T? ValueAs(object? value) => value is T typedValue ? typedValue : default;
        protected virtual T? GetDefaultValue() => default;

        object? IEWParameterConfiguration.GetDefaultValue()
        {
            return GetDefaultValue();
        }
    }
}
