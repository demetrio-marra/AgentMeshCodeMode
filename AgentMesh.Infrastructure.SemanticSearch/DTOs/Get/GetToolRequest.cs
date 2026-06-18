using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SemanticSearch.DTOs.Get
{
    /// <summary>
    /// Arguments for the MCP <c>get</c> tool — retrieves the full content of a document by file path or docid.
    /// </summary>
    public class GetToolRequest
    {
        /// <summary>
        /// File path or docid from search results. Supports a line-range suffix:
        /// <c>pages/meeting.md:100</c> starts at line 100;
        /// <c>pages/meeting.md:100:40</c> (or <c>#abc123:100:40</c>) reads 40 lines from line 100.
        /// </summary>
        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        /// <summary>
        /// Start from this line number (1-indexed).
        /// </summary>
        [JsonPropertyName("fromLine")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? FromLine { get; set; }

        /// <summary>
        /// Maximum number of lines to return.
        /// </summary>
        [JsonPropertyName("maxLines")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxLines { get; set; }

        /// <summary>
        /// Add line numbers to output (format: <c>N: content</c>). Server default: true.
        /// </summary>
        [JsonPropertyName("lineNumbers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? LineNumbers { get; set; }
    }
}
