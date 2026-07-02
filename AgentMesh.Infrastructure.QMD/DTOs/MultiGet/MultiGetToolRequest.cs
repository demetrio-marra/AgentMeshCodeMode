using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.QMD.DTOs.MultiGet
{
    /// <summary>
    /// Arguments for the MCP <c>multi_get</c> tool — retrieves multiple documents by glob pattern or comma-separated list.
    /// </summary>
    public class MultiGetToolRequest
    {
        /// <summary>
        /// Glob pattern or comma-separated list of file paths.
        /// </summary>
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Maximum lines per file.
        /// </summary>
        [JsonPropertyName("maxLines")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxLines { get; set; }

        /// <summary>
        /// Skip files larger than this (server default: 10240 = 10KB).
        /// </summary>
        [JsonPropertyName("maxBytes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxBytes { get; set; }

        /// <summary>
        /// Add line numbers to output (format: <c>N: content</c>). Server default: true.
        /// </summary>
        [JsonPropertyName("lineNumbers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? LineNumbers { get; set; }
    }
}
