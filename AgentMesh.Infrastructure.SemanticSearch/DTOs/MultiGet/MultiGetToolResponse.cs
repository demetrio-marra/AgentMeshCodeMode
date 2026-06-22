using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SemanticSearch.DTOs.MultiGet
{
    /// <summary>
    /// Response payload from the MCP <c>multi_get</c> tool. Contains an entry per file that matched the pattern.
    /// </summary>
    public class MultiGetToolResponse
    {
        public IEnumerable<MultiGetToolResponseItem> Files { get; set; } = [];
    }

    public class MultiGetToolResponseItem
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
