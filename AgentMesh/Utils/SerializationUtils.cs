using System.Text.Json;

namespace AgentMesh.Utils
{
    public static class SerializationUtils
    {
        public static readonly JsonSerializerOptions DefaultSerializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            },
            IgnoreReadOnlyProperties = true
        };

        public static readonly JsonSerializerOptions DefaultDeserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            }
        };
    }
}
