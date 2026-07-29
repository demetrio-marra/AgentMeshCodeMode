using AgentMesh.Models.Workflows;
using AgentMesh.Utils;
using System.Text.Json;

namespace AgentMesh.Models.Parameters
{
    public class Parameter
    {
        public const string NoDisplayValue = "(None)";
        public const string NoValueForLLM = "(None)";

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Set true only for the parameter that is used to store the conversation history for the workflow.
        /// </summary>
        public bool IsConversationHistoryParameter { get; set; }

        /// <summary>
        /// Set true only for the parameter that is used to store the last request from the user.
        /// </summary>
        public bool IsUserCurrentRequestParameter { get; set; }

        /// <summary>
        /// Set true only for the parameter that is used to store the response for the user.
        /// </summary>
        public bool IsResponseForUserParameter { get; set; }

        public string? RawValue { get; set; }

        public Func<string?, string> GetDisplayValue { get; set; } = (rawValue) => rawValue ?? NoDisplayValue;

        public string ValueForLLM => string.IsNullOrWhiteSpace(RawValue) ? NoValueForLLM : RawValue;

        public static T? AsObject<T>(string? rawValue)
        {
            if (rawValue == null)
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(rawValue, SerializationUtils.DefaultDeserializeOptions);
        }

        public void SetValue<T>(T value)
        {
            RawValue = JsonSerializer.Serialize(value, SerializationUtils.DefaultSerializeOptions);
        }

        public ParameterRecord ToParameterRecord()     
        {
            return new ParameterRecord(Name, RawValue, ValueForLLM, GetDisplayValue(RawValue));
        }
    }
}
