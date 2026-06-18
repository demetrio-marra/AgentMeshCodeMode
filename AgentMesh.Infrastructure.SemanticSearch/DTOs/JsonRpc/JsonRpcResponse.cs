using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SemanticSearch.DTOs.JsonRpc
{
    /// <summary>
    /// Generic JSON-RPC 2.0 response envelope.
    /// </summary>
    internal class JsonRpcResponse<TResult>
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public object? Id { get; set; }

        [JsonPropertyName("result")]
        public TResult? Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcError? Error { get; set; }
    }

    internal class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}
