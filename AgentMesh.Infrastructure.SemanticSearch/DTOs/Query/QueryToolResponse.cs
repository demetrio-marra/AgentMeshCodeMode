using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SemanticSearch.DTOs.Query
{
    /// <summary>
    /// Response payload from the MCP <c>query</c> tool. The server's exact response shape
    /// is not part of the published input schema, so unknown fields are preserved via
    /// <see cref="Extensions"/> and the typed properties only cover the documented ones.
    /// </summary>
    public class QueryToolResponse
    {
        /// <summary>
        /// Matching documents ordered by relevance.
        /// </summary>
        [JsonPropertyName("results")]
        public List<QueryToolResultItem> Results { get; set; } = new();

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }

    public class QueryToolResultItem
    {
        /// <summary>
        /// Source file path of the match (relative to the knowledge base root).
        /// </summary>
        [JsonPropertyName("file")]
        public string? File { get; set; }

        /// <summary>
        /// Document id (e.g. <c>#abc123</c>) usable with the <c>get</c> tool.
        /// </summary>
        [JsonPropertyName("docid")]
        public string? DocId { get; set; }

        /// <summary>
        /// Absolute 1-indexed line of the best match in the source markdown.
        /// </summary>
        [JsonPropertyName("line")]
        public int? Line { get; set; }

        /// <summary>
        /// Relevance score assigned by the server (typically 0-1).
        /// </summary>
        [JsonPropertyName("score")]
        public double? Score { get; set; }

        /// <summary>
        /// Short snippet of the matching text, if returned by the server.
        /// </summary>
        [JsonPropertyName("snippet")]
        public string? Snippet { get; set; }

        /// <summary>
        /// Title of the matching document, if returned by the server.
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Collection this match belongs to, if returned by the server.
        /// </summary>
        [JsonPropertyName("collection")]
        public string? Collection { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }
}
