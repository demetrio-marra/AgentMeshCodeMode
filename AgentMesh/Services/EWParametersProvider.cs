using AgentMesh.Models.Workflows;
using System.Collections.Concurrent;

namespace AgentMesh.Services
{
    /// <summary>
    /// Provides a centralized way to manage and access workflow parameters using EWParameter models.
    /// It uses a concurrent dictionary to store parameters, allowing for thread-safe operations.
    /// The class allows retrieval of parameters by name, setting parameter values, and updating multiple parameters at once.
    /// </summary>
    public class EWParametersProvider
    {
        private readonly ConcurrentDictionary<string, IEWParameter> _parameters;

        public EWParametersProvider(IEnumerable<IEWParameter> parameters)
        {
            _parameters = new ConcurrentDictionary<string, IEWParameter>(
                parameters.ToDictionary(p => p.Name, p => p),
                StringComparer.InvariantCultureIgnoreCase);
        }

        public IEnumerable<IEWParameter> GetParameters()
        {
            return _parameters.Values.AsEnumerable();
        }

        public IEnumerable<IEWParameter> GetParameters(IEnumerable<string> names)
        {
            return _parameters
                .Where(kv => names.Contains(kv.Key, StringComparer.InvariantCultureIgnoreCase))
                .Select(kv => kv.Value);
        }

        public void UpdateParameterValue<T>(string name, T? value)
        {
            if (_parameters.TryGetValue(name, out var parameter))
            {
                if (parameter is EWParameter<T> typedParameter)
                {
                    typedParameter.ParameterValue = value;
                    _parameters[name] = typedParameter;
                }
                else
                {
                    throw new ArgumentException($"Parameter with name '{name}' is not of type {typeof(T).Name}.");
                }
            }
            else
            {
                throw new ArgumentException($"Parameter with name '{name}' does not exist.");
            }
        }

        public IEWParameter? GetUserCurrentRequestParameter() => GetParameterByPredicate(p => p.IsUserCurrentRequestParameter);
        public IEWParameter? GetConversationHistoryParameter() => GetParameterByPredicate(p => p.IsConversationHistoryParameter);
        public IEWParameter? GetResponseForUserParameter() => GetParameterByPredicate(p => p.IsResponseForUserParameter);

        private IEWParameter? GetParameterByPredicate(Func<IEWParameter, bool> predicate)
        {
            return _parameters.Values.FirstOrDefault(predicate);
        }
    }
}
