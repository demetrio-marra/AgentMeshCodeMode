using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.QMD.DTOs.JsonRpc
{
    /// <summary>
    /// Params for the MCP <c>initialize</c> method.
    /// </summary>
    internal class InitializeParams
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = string.Empty;

        [JsonPropertyName("capabilities")]
        public JsonElement? Capabilities { get; set; }

        [JsonPropertyName("clientInfo")]
        public ClientInfo ClientInfo { get; set; } = new();
    }

    internal class ClientInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of the MCP <c>initialize</c> method. We only need the protocol version /
    /// server info to validate the handshake; unknown properties are preserved.
    /// </summary>
    internal class InitializeResult
    {
        [JsonPropertyName("protocolVersion")]
        public string? ProtocolVersion { get; set; }

        [JsonPropertyName("serverInfo")]
        public JsonElement? ServerInfo { get; set; }

        [JsonPropertyName("capabilities")]
        public JsonElement? Capabilities { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }
}
