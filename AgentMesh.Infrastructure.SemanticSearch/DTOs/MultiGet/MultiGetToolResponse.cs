using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SemanticSearch.DTOs.MultiGet
{
    /// <summary>
    /// Response payload from the MCP <c>multi_get</c> tool. Contains an entry per file that matched the pattern.
    /// </summary>
    public class MultiGetToolResponse
    {
        [JsonPropertyName("files")]
        public List<MultiGetFileItem> Files { get; set; } = new();

        /// <summary>
        /// Files that were skipped (e.g. because they exceeded <c>maxBytes</c>).
        /// </summary>
        [JsonPropertyName("skipped")]
        public List<string>? Skipped { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }

    public class MultiGetFileItem
    {
        [JsonPropertyName("file")]
        public string? File { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("lines")]
        public int? Lines { get; set; }

        [JsonPropertyName("bytes")]
        public long? Bytes { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }
}
