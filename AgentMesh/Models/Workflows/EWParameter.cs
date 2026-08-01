using AgentMesh.Utils;
using System.Reflection.Metadata;
using System.Text.Json;

namespace AgentMesh.Models.Workflows
{
    public abstract class EWParameter<T> : IEWParameter
    {
        public const string NoDataPlaceholder = "(None)";

        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Set true only for the parameter that is used to store the conversation history for the workflow.
        /// </summary>
        public bool IsConversationHistoryParameter { get; init; }

        /// <summary>
        /// Set true only for the parameter that is used to store the last request from the user.
        /// </summary>
        public bool IsUserCurrentRequestParameter { get; init; }

        /// <summary>
        /// Set true only for the parameter that is used to store the response for the user.
        /// </summary>
        public bool IsResponseForUserParameter { get; init; }

        public T? ParameterValue { get; set; }

        /// <summary>
        /// A function that takes the parameter value and returns a string representation for visualization purposes.
        /// By default, it serializes the object to JSON using the default serialization options.
        /// </summary>
        public Func<T?, string> SerializeForVisualization { get; init; } = (ob) =>
        {
            if (ob == null)
            {
                return NoDataPlaceholder;
            }
            else
            {
                if (ob is string strValue)
                {
                    if (string.IsNullOrWhiteSpace(strValue))
                    {
                        return NoDataPlaceholder;
                    }
                    return strValue;
                }
                else if (ob.GetType().IsPrimitive)
                {
                    return ob.ToString() ?? NoDataPlaceholder;
                }
                else
                {
                    return JsonSerializer.Serialize(ob, SerializationUtils.DefaultSerializeOptions);
                }
            }
        };

        public string DisplayValue => SerializeForVisualization(ParameterValue);

        public string? RawSerializedValue
        {
            get
            {
                if (ParameterValue == null)
                {
                    return null;
                }
                else if (ParameterValue is string strValue)
                {
                    return strValue;
                }
                else if (ParameterValue.GetType().IsPrimitive)
                {
                    return ParameterValue?.ToString() ?? NoDataPlaceholder;
                }
                else
                {
                    return JsonSerializer.Serialize(ParameterValue, SerializationUtils.DefaultSerializeOptions);
                }
            }
        }

        public EWDisplayParameterRecord ToDisplayParameterRecord()
        {
            return new EWDisplayParameterRecord(Name, SerializeForVisualization(ParameterValue));
        }
    }
}
