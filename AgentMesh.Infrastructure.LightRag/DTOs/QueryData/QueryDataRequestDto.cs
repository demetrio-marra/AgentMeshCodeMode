using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.LightRag.DTOs.QueryData
{
    public class QueryDataRequestDto
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("mode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Mode { get; set; }

        [JsonPropertyName("top_k")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TopK { get; set; }

        [JsonPropertyName("chunk_top_k")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ChunkTopK { get; set; }

        [JsonPropertyName("hl_keywords")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? HighLevelKeywords { get; set; }

        [JsonPropertyName("ll_keywords")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? LowLevelKeywords { get; set; }

        [JsonPropertyName("include_references")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IncludeReferences { get; set; }

        [JsonPropertyName("include_chunk_content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IncludeChunkContent { get; set; }
    }
}
