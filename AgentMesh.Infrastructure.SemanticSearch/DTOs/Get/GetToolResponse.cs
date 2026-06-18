using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SemanticSearch.DTOs.Get
{
    /// <summary>
    /// Response payload from the MCP <c>get</c> tool. When the server returns plain text
    /// (the common case), <see cref="Content"/> holds the document body and the structured
    /// fields will be null. When the server returns a structured object, its fields are
    /// captured below and any unknown ones in <see cref="Extensions"/>.
    /// </summary>
    public class GetToolResponse
    {
        /// <summary>
        /// Raw document content as returned by the server.
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>
        /// File path the server resolved the request to.
        /// </summary>
        [JsonPropertyName("file")]
        public string? File { get; set; }

        /// <summary>
        /// 1-indexed line where the returned content starts.
        /// </summary>
        [JsonPropertyName("fromLine")]
        public int? FromLine { get; set; }

        /// <summary>
        /// Number of lines actually returned.
        /// </summary>
        [JsonPropertyName("lines")]
        public int? Lines { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }
}
