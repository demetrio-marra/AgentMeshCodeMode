using System.Text.Json;

namespace AgentMesh.Application
{
    public static class AgentResponseJsonSerializationUtils
    {
     
        public static JsonSerializerOptions DefaultSerializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public static JsonSerializerOptions DefaultDeserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
