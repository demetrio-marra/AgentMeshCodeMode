using AgentMesh.Utils;
using System.Text.Json;

namespace AgentMesh.Models.Parameters
{
    public class Parameter
    {
        public const string NoDisplayValue = "(None)";
        public const string NoValueForLLM = "(None)";

        public string Name { get; set; } = string.Empty;

        public string? RawValue { get; set; }

        public bool IsSystemProvided { get; set; }

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
    }
}
