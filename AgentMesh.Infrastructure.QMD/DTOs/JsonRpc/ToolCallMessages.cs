using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.QMD.DTOs.JsonRpc
{
    /// <summary>
    /// Params for the MCP <c>tools/call</c> method. <typeparamref name="TArguments"/> is the
    /// tool-specific arguments DTO (e.g. <see cref="Query.QueryToolRequest"/>).
    /// </summary>
    internal class ToolCallParams<TArguments>
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("arguments")]
        public TArguments? Arguments { get; set; }
    }

    /// <summary>
    /// Result of the MCP <c>tools/call</c> method. Servers may return either
    /// <c>structuredContent</c> (preferred for typed payloads) or a textual content
    /// array. Both are exposed so the proxy can pick whichever is present.
    /// </summary>
    internal class ToolCallResult
    {
        [JsonPropertyName("content")]
        public List<ToolContentItem>? Content { get; set; }

        [JsonPropertyName("structuredContent")]
        public JsonElement? StructuredContent { get; set; }

        [JsonPropertyName("isError")]
        public bool? IsError { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }

    internal class ToolContentItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }
}
