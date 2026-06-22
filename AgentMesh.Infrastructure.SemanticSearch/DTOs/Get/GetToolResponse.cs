using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SemanticSearch.DTOs.Get
{

    public class GetToolResponse
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

    }
}
