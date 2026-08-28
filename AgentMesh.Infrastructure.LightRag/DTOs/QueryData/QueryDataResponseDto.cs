using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.LightRag.DTOs.QueryData
{
    public class QueryDataResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public QueryDataPayloadDto Data { get; set; } = new();

        [JsonPropertyName("metadata")]
        public Dictionary<string, JsonElement>? Metadata { get; set; }
    }

    public class QueryDataPayloadDto
    {
        [JsonPropertyName("entities")]
        public List<QueryDataEntityDto> Entities { get; set; } = [];

        [JsonPropertyName("relationships")]
        public List<QueryDataRelationshipDto> Relationships { get; set; } = [];

        [JsonPropertyName("chunks")]
        public List<QueryDataChunkDto> Chunks { get; set; } = [];

        [JsonPropertyName("references")]
        public List<QueryDataReferenceDto> References { get; set; } = [];

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }
    }

    public class QueryDataEntityDto
    {
        [JsonPropertyName("entity_name")]
        public string EntityName { get; set; } = string.Empty;

        [JsonPropertyName("entity_type")]
        public string? EntityType { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }

        [JsonPropertyName("file_path")]
        public string? FilePath { get; set; }

        [JsonPropertyName("reference_id")]
        public string? ReferenceId { get; set; }
    }

    public class QueryDataRelationshipDto
    {
        [JsonPropertyName("src_id")]
        public string SourceEntity { get; set; } = string.Empty;

        [JsonPropertyName("tgt_id")]
        public string TargetEntity { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("keywords")]
        public string? Keywords { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }

        [JsonPropertyName("file_path")]
        public string? FilePath { get; set; }

        [JsonPropertyName("reference_id")]
        public string? ReferenceId { get; set; }
    }

    public class QueryDataChunkDto
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("file_path")]
        public string? FilePath { get; set; }

        [JsonPropertyName("chunk_id")]
        public string? ChunkId { get; set; }

        [JsonPropertyName("reference_id")]
        public string? ReferenceId { get; set; }
    }

    public class QueryDataReferenceDto
    {
        [JsonPropertyName("reference_id")]
        public string? ReferenceId { get; set; }

        [JsonPropertyName("file_path")]
        public string? FilePath { get; set; }
    }
}
