using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.QMD.DTOs.JsonRpc
{
    /// <summary>
    /// Generic JSON-RPC 2.0 request envelope used by every MCP call.
    /// </summary>
    internal class JsonRpcRequest<TParams>
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TParams? Params { get; set; }
    }
}
