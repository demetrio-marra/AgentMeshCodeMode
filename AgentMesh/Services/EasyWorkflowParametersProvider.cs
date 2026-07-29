using AgentMesh.Models.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Utils;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AgentMesh.Services
{
    /// <summary>
    /// Provides a centralized way to manage and access workflow parameters. 
    /// It uses a concurrent dictionary to store parameters, allowing for thread-safe operations.
    /// The class allows retrieval of parameters by name, setting parameter values, and updating multiple parameters at once.
    /// It relies on an external factory to create the initial set of parameters.
    /// </summary>
    public class EasyWorkflowParametersProvider
    {
        private readonly ConcurrentDictionary<string, Parameter> _parameters;

        public EasyWorkflowParametersProvider(IEasyWorkflowParametersFactory parametersFactory)
        {
            _parameters = new ConcurrentDictionary<string, Parameter>(parametersFactory.CreateParameters().ToDictionary(p => p.Name, p => p),
                StringComparer.InvariantCultureIgnoreCase);
        }

        public IEnumerable<ParameterRecord> GetParameters()
        {
            return _parameters.Select(kv => kv.Value)
                              .Select(p => new ParameterRecord(p.Name, p.RawValue, p.ValueForLLM, p.GetDisplayValue(p.RawValue)));
        }

        public IEnumerable<ParameterRecord> GetParameters(IEnumerable<string> names)
        {
            return _parameters.Where(kv => names.Contains(kv.Key, StringComparer.InvariantCultureIgnoreCase))
                              .Select(kv => kv.Value)
                              .Select(p => new ParameterRecord(p.Name, p.RawValue, p.ValueForLLM, p.GetDisplayValue(p.RawValue)));
        }

        public ParameterRecord? GetParameter(string name)
        {
            if (_parameters.TryGetValue(name, out var parameter))
            {
                return new ParameterRecord(parameter.Name, parameter.RawValue, parameter.ValueForLLM, parameter.GetDisplayValue(parameter.RawValue));
            }
            return null;
        }

        public void SetParameterValue(string name, string? rawValue)
        {
            if (_parameters.TryGetValue(name, out var parameter))
            {
                parameter.RawValue = rawValue;
                _parameters[name] = parameter; // Update the parameter in the dictionary
            }
            else
            {
                throw new ArgumentException($"Parameter with name '{name}' does not exist. Check the parameters factory for available parameters.");
            }
        }

        public void SetParameterValue<T>(string name, T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            }

            var stringifiedParameter = JsonSerializer.Serialize(value, SerializationUtils.DefaultSerializeOptions);
            SetParameterValue(name, stringifiedParameter);
        }

        public void SetParameters(IEnumerable<ParameterRecord> parameterRecords)
        {
            foreach (var record in parameterRecords)
            {
                SetParameterValue(record.Name, record.RawValue);
            }
        }

        public ParameterRecord? GetUserCurrentRequestParameter() => GetParameterByPredicate(p => p.IsUserCurrentRequestParameter);
        public ParameterRecord? GetConversationHistoryParameter() => GetParameterByPredicate(p => p.IsConversationHistoryParameter);
        public ParameterRecord? GetResponseForUserParameter() => GetParameterByPredicate(p => p.IsResponseForUserParameter);

        private ParameterRecord? GetParameterByPredicate(Func<Parameter, bool> predicate)
        {
            var parameter = _parameters.Values.FirstOrDefault(predicate);
            return parameter?.ToParameterRecord();
        }
    }
}
