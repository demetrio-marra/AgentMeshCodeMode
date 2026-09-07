using AgentMesh.Models;
using AgentMesh.Services;
using System.Reflection;

namespace AgentMesh.Helpers
{
    internal static class AssemblyDiscoveryHelper
    {
        internal static IEnumerable<Type> DiscoverEWParameterImplementations()
        {
            return GetAllAssemblies()
                .SelectMany(GetTypesSafely)
                .Where(IsEWParameterConfiguration)
                .Distinct();
        }

        internal static IEnumerable<Type> DiscoverEWStepImplementations()
        {
            return GetAllAssemblies()
                .SelectMany(GetTypesSafely)
                .Where(IsConcreteEWStep)
                .Distinct();
        }

        internal static IEnumerable<Type> DiscoverEWAgentImplementations()
        {
            return GetAllAssemblies()
                .SelectMany(GetTypesSafely)
                .Where(IsConcreteEWAgent)
                .Distinct();
        }

        private static bool IsEWParameterConfiguration(Type type)
        {
            return type.IsClass
                && !type.IsAbstract
                && !type.ContainsGenericParameters
                && typeof(IEWParameterConfiguration).IsAssignableFrom(type);
        }

        private static bool IsConcreteEWStep(Type type)
        {
            return type.IsClass
                && !type.IsAbstract
                && !type.ContainsGenericParameters
                && typeof(IEWStep).IsAssignableFrom(type);
        }

        private static bool IsConcreteEWAgent(Type type)
        {
            return type.IsClass
                && !type.IsAbstract
                && !type.ContainsGenericParameters
                && typeof(IEWAgent).IsAssignableFrom(type);
        }

        private static IEnumerable<Assembly> GetAllAssemblies()
        {
            var discoveredAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<Assembly>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!discoveredAssemblies.ContainsKey(assembly.FullName ?? assembly.GetName().Name ?? string.Empty))
                {
                    discoveredAssemblies[assembly.FullName ?? assembly.GetName().Name ?? string.Empty] = assembly;
                    queue.Enqueue(assembly);
                }
            }

            while (queue.Count > 0)
            {
                var assembly = queue.Dequeue();
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    if (discoveredAssemblies.ContainsKey(reference.FullName))
                    {
                        continue;
                    }

                    try
                    {
                        var loadedAssembly = Assembly.Load(reference);
                        discoveredAssemblies[reference.FullName] = loadedAssembly;
                        queue.Enqueue(loadedAssembly);
                    }
                    catch
                    {
                        // Ignore assemblies that cannot be loaded.
                    }
                }
            }

            return discoveredAssemblies.Values;
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!;
            }
            catch
            {
                return [];
            }
        }
    }
}
