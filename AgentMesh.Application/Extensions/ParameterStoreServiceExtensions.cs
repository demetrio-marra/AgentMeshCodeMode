using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering the parameter store and related services.
    /// </summary>
    public static class ParameterStoreServiceExtensions
    {
        /// <summary>
        /// Registers the parameter store as a singleton.
        /// Must be called during service configuration before any steps are registered.
        /// 
        /// Usage:
        ///   services.AddParameterStore();
        /// </summary>
        public static IServiceCollection AddParameterStore(this IServiceCollection services)
        {
            services.AddSingleton<ParameterStore>();
            services.AddSingleton<IParameterStore>(sp => sp.GetRequiredService<ParameterStore>());
            return services;
        }

        /// <summary>
        /// Registers a parameter type with initial value in the parameter store.
        /// Called automatically during parameter discovery.
        /// 
        /// This ensures the store knows about all parameters before any step execution.
        /// </summary>
        public static void RegisterParameterWithStore(
            this IServiceProvider serviceProvider,
            Type parameterType,
            object? initialValue = null)
        {
            var store = serviceProvider.GetRequiredService<ParameterStore>();
            store.RegisterParameterDefinition(parameterType, initialValue);
        }
    }
}
