using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SemanticSearch.DTOs.Status
{
    /// <summary>
    /// Response payload from the MCP <c>status</c> tool — collections, document counts and health information.
    /// </summary>
    public class StatusToolResponse
    {
        /// <summary>
        /// Overall health string reported by the server (e.g. <c>ok</c>, <c>degraded</c>).
        /// </summary>
        [JsonPropertyName("health")]
        public string? Health { get; set; }

        /// <summary>
        /// Total number of documents indexed across all collections.
        /// </summary>
        [JsonPropertyName("documents")]
        public long? Documents { get; set; }

        /// <summary>
        /// Indexed collections with per-collection statistics.
        /// </summary>
        [JsonPropertyName("collections")]
        public List<StatusCollectionInfo>? Collections { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }

    public class StatusCollectionInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("documents")]
        public long? Documents { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }
}
